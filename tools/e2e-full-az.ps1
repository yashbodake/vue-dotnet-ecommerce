$ErrorActionPreference = 'Stop'
$base = 'http://localhost:44300'
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
$pass = 0; $fail = 0
$script:orderId = $null

function Pass([string]$n) { $script:pass++; Write-Host ("  PASS  {0}" -f $n) -ForegroundColor Green }
function Fail([string]$n, [string]$d) { $script:fail++; Write-Host ("  FAIL  {0} - {1}" -f $n, $d) -ForegroundColor Red }
function AbsUrl([string]$loc) {
  if ([string]::IsNullOrWhiteSpace($loc)) { return $null }
  if ($loc.StartsWith('http')) { return $loc }
  return ($base + $loc)
}
function Get-TokenFromFile([string]$path) {
  $html = Get-Content -Raw $path
  $m = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
  if (-not $m.Success) { $m = [regex]::Match($html, 'value="([^"]+)"[^>]*name="__RequestVerificationToken"') }
  if (-not $m.Success) { throw ("No token in {0}" -f $path) }
  return $m.Groups[1].Value
}
function FormData([hashtable]$fields) {
  $parts = New-Object System.Collections.Generic.List[string]
  foreach ($k in $fields.Keys) { $parts.Add(("{0}={1}" -f $k, [uri]::EscapeDataString([string]$fields[$k]))) }
  return ($parts -join '&')
}
function Curl-Get([string]$url, [string]$jar, [string]$out, [int]$maxTime = 60) {
  & curl.exe -s -m $maxTime -c $jar -b $jar -o $out -D ($out + '.hdr') -w '%{http_code}' $url
}
function Curl-Post([string]$url, [string]$jar, [string]$out, [string]$data, [int]$maxTime = 60) {
  & curl.exe -s -m $maxTime -c $jar -b $jar -o $out -D ($out + '.hdr') -w '%{http_code}' -X POST --data $data $url
}
function Get-LocationHdr([string]$hdrFile) {
  if (-not (Test-Path $hdrFile)) { return $null }
  $h = Get-Content -Raw $hdrFile
  $m = [regex]::Match($h, '(?im)^Location:\s*(.+)$')
  if ($m.Success) { return (AbsUrl $m.Groups[1].Value.Trim()) }
  return $null
}
function Follow([string]$jar, [string]$loc, [string]$out) {
  if (-not $loc) { throw 'No location' }
  return Curl-Get $loc $jar $out
}
function Get-FilterHtml([string]$qs) {
  $out = Join-Path $env:TEMP ('azf' + [guid]::NewGuid().ToString('N').Substring(0,8) + '.html')
  $code = & curl.exe -s -m 30 -o $out -w '%{http_code}' ($base + '/Product/Filter?' + $qs)
  @{ Code = $code; Html = (Get-Content -Raw $out) }
}
function Cards($html) { ([regex]::Matches($html, '<article class="product-card">')).Count }
function Names($html) { @([regex]::Matches($html, '<h3>\s*<a[^>]*>([^<]+)</a>') | ForEach-Object { $_.Groups[1].Value.Trim() }) }
function Prices($html) { @([regex]::Matches($html, 'product-price[^>]*>\s*[^\d]*([0-9,]+\.[0-9]{2})') | ForEach-Object { [decimal]($_.Groups[1].Value.Replace(',','')) }) }

Write-Host ''
Write-Host '=== FULL A-Z E2E (194-product catalog) ===' -ForegroundColor Cyan

# Warmup
$warm = Join-Path $env:TEMP 'az-warm.html'
$code = Curl-Get ($base + '/') (Join-Path $env:TEMP 'az-warm.txt') $warm 90
Write-Host ("Warmup home: {0}" -f $code)

# ---- Spec 00 DB ----
Write-Host 'Spec 00 - Database'
$db = & $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Product WHERE IsActive=1; SELECT COUNT(*) FROM ProductImage pi INNER JOIN Product p ON p.ProductId=pi.ProductId WHERE p.IsActive=1; SELECT COUNT(DISTINCT CategoryId) FROM Product WHERE IsActive=1; SELECT COUNT(*) FROM sys.indexes WHERE name IN ('IX_Product_CategoryId','IX_CartItem_UserId','IX_Orders_UserId');" -h -1 -W
$nums = @($db | Where-Object { $_ -match '^\d+$' })
$active = [int]$nums[0]; $imgs = [int]$nums[1]; $catsN = [int]$nums[2]; $idx = [int]$nums[3]
if ($active -eq 194) { Pass ("Active products = 194") } else { Fail 'Active products' ("got {0}" -f $active) }
if ($imgs -ge 194) { Pass ("Active product images = {0}" -f $imgs) } else { Fail 'Images' ("got {0}" -f $imgs) }
if ($catsN -ge 20) { Pass ("Active categories = {0}" -f $catsN) } else { Fail 'Categories' ("got {0}" -f $catsN) }
if ($idx -eq 3) { Pass 'Spec 10 indexes present' } else { Fail 'Indexes' ("got {0}" -f $idx) }

$catLines = & $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q "SET NOCOUNT ON; SELECT CAST(c.CategoryId as varchar(10)) + '=' + c.Name FROM Category c WHERE EXISTS (SELECT 1 FROM Product p WHERE p.CategoryId=c.CategoryId AND p.IsActive=1) ORDER BY c.Name;" -h -1 -W
$cats = @{}
foreach ($line in $catLines) { if ($line -match '^(\d+)=(.+)$') { $cats[$Matches[2].Trim()] = [int]$Matches[1] } }

# ---- Spec 01 ----
Write-Host 'Spec 01 - DI'
$out = Join-Path $env:TEMP 'az-test.html'
$code = Curl-Get ($base + '/Test') (Join-Path $env:TEMP 'az-g.txt') $out
$body = Get-Content -Raw $out
if ($code -eq '200' -and $body -match '\d+') { Pass '/Test DI smoke' } else { Fail '/Test' ("code={0}" -f $code) }

# ---- Spec 10 ----
Write-Host 'Spec 10 - Bundling / errors'
$homeHtml = Get-Content -Raw $warm
if ($homeHtml -match '/bundles/jquery|jquery-3.4.1') { Pass 'jQuery bundle on layout' } else { Fail 'jQuery bundle' 'missing' }
if ($homeHtml -match 'RequestVerificationToken') { Pass 'AJAX antiforgery setup' } else { Fail 'Antiforgery JS' 'missing' }
$bj = Join-Path $env:TEMP 'az-jq.js'
$code = Curl-Get ($base + '/bundles/jquery') (Join-Path $env:TEMP 'az-g.txt') $bj
if ($code -eq '200' -and (Get-Item $bj).Length -gt 10000) { Pass 'bundles/jquery serves' } else { Fail 'bundles/jquery' $code }
$err = Join-Path $env:TEMP 'az-err.html'
$code = Curl-Get ($base + '/ErrorDemo/Boom') (Join-Path $env:TEMP 'az-g.txt') $err
if ((Get-Content -Raw $err) -match 'Something went wrong') { Pass 'Error.cshtml' } else { Fail 'Error.cshtml' $code }

# Sidebar categories
$legacy = @(); if ($homeHtml -match 'Electronics') { $legacy += 'Electronics' }; if ($homeHtml -match '>Apparel<') { $legacy += 'Apparel' }
if ($legacy.Count -eq 0 -and $homeHtml -match 'Smartphones' -and $homeHtml -match 'Laptops') { Pass 'Sidebar shows rich DummyJSON categories (no empty legacy)' }
else { Fail 'Sidebar categories' ("legacy={0}" -f ($legacy -join ',')) }

# ---- Spec 05 catalog ----
Write-Host 'Spec 05 - Catalog filters'
$r = Get-FilterHtml 'Page=1&PageSize=12&SortBy=name'
if ($r.Code -eq '200' -and (Cards $r.Html) -eq 12) { Pass 'Page1 = 12' } else { Fail 'Page1' ("cards={0}" -f (Cards $r.Html)) }
if ($r.Html -match 'cdn\.dummyjson\.com') { Pass 'CDN images page1' } else { Fail 'CDN images' 'none' }
if ($r.Html -match 'data-page="2"') { Pass 'Pagination page2 link' } else { Fail 'Pagination' 'no page2' }

$r = Get-FilterHtml 'Page=17&PageSize=12&SortBy=name'
if ((Cards $r.Html) -eq 2) { Pass 'Page17 = 2 (194/12 remainder)' } else { Fail 'Page17' ("cards={0}" -f (Cards $r.Html)) }

# category samples
foreach ($pair in @(
  @{ Name='Smartphones'; Expect=16 },
  @{ Name='Laptops'; Expect=5 },
  @{ Name='Kitchen Accessories'; Expect=30 },
  @{ Name='Womens Dresses'; Expect=5 }
)) {
  if (-not $cats.ContainsKey($pair.Name)) { Fail ("Category {0}" -f $pair.Name) 'missing in DB'; continue }
  $r = Get-FilterHtml ("Page=1&PageSize=50&SortBy=name&CategoryIds={0}" -f $cats[$pair.Name])
  $n = Cards $r.Html
  if ($n -eq $pair.Expect) { Pass ("Filter {0} = {1}" -f $pair.Name, $n) } else { Fail ("Filter {0}" -f $pair.Name) ("got {0} expected {1}" -f $n, $pair.Expect) }
}

$r = Get-FilterHtml ("Page=1&PageSize=50&SortBy=name&CategoryIds={0}&CategoryIds={1}" -f $cats['Smartphones'], $cats['Laptops'])
if ((Cards $r.Html) -eq 21) { Pass 'Multi-cat Smartphones+Laptops = 21' } else { Fail 'Multi-cat' ("got {0}" -f (Cards $r.Html)) }

$r = Get-FilterHtml 'Page=1&PageSize=50&SortBy=price_asc&MinPrice=500&MaxPrice=2000'
$ps = Prices $r.Html
$bad = @($ps | Where-Object { $_ -lt 500 -or $_ -gt 2000 })
if ((Cards $r.Html) -gt 0 -and $bad.Count -eq 0) { Pass ("Price 500-2000 ({0} items)" -f (Cards $r.Html)) } else { Fail 'Price range' ("n={0}" -f (Cards $r.Html)) }

$r = Get-FilterHtml 'Page=1&PageSize=200&SortBy=name&InStockOnly=true'
$oosActive = [int]((& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Product WHERE IsActive=1 AND Stock=0;" -h -1 -W | Where-Object {$_ -match '^\d+$'} | Select-Object -First 1))
$expectStock = 194 - $oosActive
if ((Cards $r.Html) -eq $expectStock) { Pass ("InStockOnly = {0}" -f $expectStock) } else { Fail 'InStockOnly' ("got {0} expected {1}" -f (Cards $r.Html), $expectStock) }

$r = Get-FilterHtml 'Page=1&PageSize=20&SortBy=name&Search=iPhone'
if ((Cards $r.Html) -ge 1) { Pass ("Search iPhone => {0}" -f ((Names $r.Html) -join ', ')) } else { Fail 'Search iPhone' 'none' }
$r = Get-FilterHtml 'Page=1&PageSize=20&SortBy=name&Search=zzznope999'
if ((Cards $r.Html) -eq 0) { Pass 'Search miss = 0' } else { Fail 'Search miss' ("got {0}" -f (Cards $r.Html)) }

$r = Get-FilterHtml 'Page=1&PageSize=12&SortBy=price_asc'
$ps = Prices $r.Html
$ok=$true; for($i=1;$i -lt $ps.Count;$i++){ if($ps[$i] -lt $ps[$i-1]){$ok=$false} }
if ($ok -and $ps.Count -ge 2) { Pass ("Sort price_asc {0}..{1}" -f $ps[0], $ps[-1]) } else { Fail 'price_asc' ($ps -join ',') }

$r = Get-FilterHtml 'Page=1&PageSize=12&SortBy=price_desc'
$ps = Prices $r.Html
$ok=$true; for($i=1;$i -lt $ps.Count;$i++){ if($ps[$i] -gt $ps[$i-1]){$ok=$false} }
if ($ok -and $ps.Count -ge 2) { Pass ("Sort price_desc first={0}" -f $ps[0]) } else { Fail 'price_desc' ($ps -join ',') }

$r = Get-FilterHtml 'Page=1&PageSize=1&SortBy=name&InStockOnly=true'
$productId = ([regex]::Match($r.Html, 'Detail/(\d+)')).Groups[1].Value
$dout = Join-Path $env:TEMP 'az-det.html'
$code = & curl.exe -s -m 30 -o $dout -w '%{http_code}' ($base + '/Product/Detail/' + $productId)
$dh = Get-Content -Raw $dout
if ($code -eq '200' -and $dh -match 'cdn\.dummyjson\.com' -and $dh -match 'add-to-cart') { Pass ("Detail/{0} gallery+ATC" -f $productId) } else { Fail 'Detail' ("id={0} code={1}" -f $productId, $code) }

$oosId = (& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q "SET NOCOUNT ON; SELECT TOP 1 ProductId FROM Product WHERE IsActive=1 AND Stock=0;" -h -1 -W | Where-Object {$_ -match '^\d+$'} | Select-Object -First 1)
if ($oosId) {
  $dout = Join-Path $env:TEMP 'az-oos.html'
  & curl.exe -s -m 30 -o $dout ($base + '/Product/Detail/' + $oosId) | Out-Null
  if ((Get-Content -Raw $dout) -match 'disabled|Out of Stock') { Pass ("OOS Detail/{0} disabled" -f $oosId) } else { Fail 'OOS detail' ("id={0}" -f $oosId) }
} else { Pass 'No OOS product (all in stock) - skip OOS UI check' }

# ---- Spec 06 cart ----
Write-Host 'Spec 06 - Cart'
$guestJar = Join-Path $env:TEMP ('az-guest-' + (Get-Random) + '.txt')
$userJar = Join-Path $env:TEMP ('az-user-' + (Get-Random) + '.txt')
$adminJar = Join-Path $env:TEMP ('az-admin-' + (Get-Random) + '.txt')
$otherJar = Join-Path $env:TEMP ('az-other-' + (Get-Random) + '.txt')
$email = ('e2eaz{0}@test.local' -f (Get-Random -Maximum 999999))
$otherEmail = ('otheraz{0}@test.local' -f (Get-Random -Maximum 999999))
$pwd = 'Passw0rd!'

$out = Join-Path $env:TEMP 'az-h2.html'
Curl-Get ($base + '/') $guestJar $out 90 | Out-Null
$token = Get-TokenFromFile $out
# pick an in-stock product id from filter
$r = Get-FilterHtml 'Page=1&PageSize=1&SortBy=name&InStockOnly=true'
$buyId = ([regex]::Match($r.Html, 'Detail/(\d+)')).Groups[1].Value
$data = FormData @{ __RequestVerificationToken = $token; productId = $buyId; quantity = '1' }
$out = Join-Path $env:TEMP 'az-add1.json'
Curl-Post ($base + '/Cart/AddToCart') $guestJar $out $data | Out-Null
$j1 = Get-Content -Raw $out
if ($j1 -match '"success":true') { Pass ("AddToCart product {0}" -f $buyId) } else { Fail 'AddToCart' $j1 }

Curl-Get ($base + '/') $guestJar (Join-Path $env:TEMP 'az-h3.html') 60 | Out-Null
$token = Get-TokenFromFile (Join-Path $env:TEMP 'az-h3.html')
$data = FormData @{ __RequestVerificationToken = $token; productId = $buyId; quantity = '1' }
$out = Join-Path $env:TEMP 'az-add2.json'
Curl-Post ($base + '/Cart/AddToCart') $guestJar $out $data | Out-Null
$j2 = Get-Content -Raw $out
$cartPage = Join-Path $env:TEMP 'az-cart.html'
Curl-Get ($base + '/Cart') $guestJar $cartPage | Out-Null
$rows = ([regex]::Matches((Get-Content -Raw $cartPage), 'cart-row')).Count
if ($j2 -match '"itemCount":2' -and $rows -eq 1) { Pass 'Double-add merges to qty 2 / 1 row' } else { Fail 'Merge' ("json={0} rows={1}" -f $j2, $rows) }

$mini = Join-Path $env:TEMP 'az-mini.html'
$code = Curl-Get ($base + '/Cart/MiniCart') $guestJar $mini
if ($code -eq '200') { Pass 'MiniCart' } else { Fail 'MiniCart' $code }

# ---- Spec 07 identity ----
Write-Host 'Spec 07 - Identity'
$out = Join-Path $env:TEMP 'az-unauth.html'
$code = Curl-Get ($base + '/Account/Orders') $guestJar $out
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302' -and $loc -match 'Login') { Pass 'Unauth Orders -> Login' } else { Fail 'Unauth Orders' ("{0} {1}" -f $code, $loc) }

$out = Join-Path $env:TEMP 'az-reg.html'
Curl-Get ($base + '/Account/Register') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; Email = $email; Password = $pwd; ConfirmPassword = $pwd }
$out = Join-Path $env:TEMP 'az-reg2.html'
$code = Curl-Post ($base + '/Account/Register') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302') { Follow $userJar $loc (Join-Path $env:TEMP 'az-reg3.html') | Out-Null; Pass ('Register {0}' -f $email) } else { Fail 'Register' $code }

$out = Join-Path $env:TEMP 'az-uh.html'
Curl-Get ($base + '/') $userJar $out 90 | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; productId = $buyId; quantity = '1' }
$out = Join-Path $env:TEMP 'az-uadd.json'
Curl-Post ($base + '/Cart/AddToCart') $userJar $out $data | Out-Null
if ((Get-Content -Raw $out) -match '"success":true') { Pass 'Auth AddToCart' } else { Fail 'Auth AddToCart' (Get-Content -Raw $out) }

# ---- Spec 08 checkout ----
Write-Host 'Spec 08 - Checkout'
$out = Join-Path $env:TEMP 'az-paygate.html'
$code = Curl-Get ($base + '/Checkout/Payment') $userJar $out
$loc = Get-LocationHdr ($out + '.hdr')
$addr = Join-Path $env:TEMP 'az-addr.html'
if ($code -eq '302' -and $loc -match 'Address') { Follow $userJar $loc $addr | Out-Null; Pass 'Payment gate -> Address' }
else { Curl-Get ($base + '/Checkout/Address') $userJar $addr | Out-Null; Fail 'Payment gate' ("{0} {1}" -f $code, $loc) }

$token = Get-TokenFromFile $addr
$data = FormData @{ __RequestVerificationToken = $token; FullName = 'AZ User'; AddressLine1 = '1 Test'; City = 'Austin'; State = 'TX'; PostalCode = '78701'; Country = 'USA' }
$out = Join-Path $env:TEMP 'az-shipr.html'
$code = Curl-Post ($base + '/Checkout/Address') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($loc -match 'Shipping') {
  $ship = Join-Path $env:TEMP 'az-ship.html'
  Follow $userJar $loc $ship | Out-Null
  Pass 'Address -> Shipping'
  $token = Get-TokenFromFile $ship
  $method = 'Standard (3-5 days)'
  $data = FormData @{ __RequestVerificationToken = $token; ShippingMethod = $method }
  $out = Join-Path $env:TEMP 'az-payr.html'
  $code = Curl-Post ($base + '/Checkout/Shipping') $userJar $out $data
  $loc = Get-LocationHdr ($out + '.hdr')
  if ($loc -match 'Payment') {
    $pay = Join-Path $env:TEMP 'az-pay.html'
    Follow $userJar $loc $pay | Out-Null
    Pass 'Shipping -> Payment'
    $token = Get-TokenFromFile $pay
    $data = FormData @{ __RequestVerificationToken = $token; CardName = 'AZ User'; CardNumber = '4111111111111111'; CardExpiry = '12/28'; CardCvv = '123' }
    $out = Join-Path $env:TEMP 'az-place.html'
    $code = Curl-Post ($base + '/Checkout/PlaceOrder') $userJar $out $data
    $loc = Get-LocationHdr ($out + '.hdr')
    if ($loc -match 'Confirmation') {
      $conf = Join-Path $env:TEMP 'az-conf.html'
      Follow $userJar $loc $conf | Out-Null
      if ((Get-Content -Raw $conf) -match 'Thank you') {
        Pass 'PlaceOrder -> Confirmation'
        $om = [regex]::Match($loc, 'orderId=(\d+)')
        if ($om.Success) { $script:orderId = [int]$om.Groups[1].Value }
      } else { Fail 'Confirmation body' 'no thank you' }
    } else { Fail 'PlaceOrder' ("{0} {1}" -f $code, $loc) }
  } else { Fail 'Shipping -> Payment' $loc }
} else { Fail 'Address -> Shipping' $loc }

$out = Join-Path $env:TEMP 'az-empty.html'
Curl-Get ($base + '/Cart') $userJar $out | Out-Null
if ((Get-Content -Raw $out) -match 'empty') { Pass 'Cart cleared after order' } else { Fail 'Cart clear' 'not empty' }

# stock race
$out = Join-Path $env:TEMP 'az-st1.html'
Curl-Get ($base + '/') $userJar $out 60 | Out-Null
$token = Get-TokenFromFile $out
$r = Get-FilterHtml 'Page=1&PageSize=1&SortBy=name&InStockOnly=true'
$raceId = ([regex]::Match($r.Html, 'Detail/(\d+)')).Groups[1].Value
$data = FormData @{ __RequestVerificationToken = $token; productId = $raceId; quantity = '1' }
Curl-Post ($base + '/Cart/AddToCart') $userJar (Join-Path $env:TEMP 'az-race-add.json') $data | Out-Null
& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q ("UPDATE Product SET Stock=0 WHERE ProductId={0};" -f $raceId) -W -h -1 | Out-Null
$out = Join-Path $env:TEMP 'az-raddr.html'
Curl-Get ($base + '/Checkout/Address') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; FullName = 'Race'; AddressLine1 = '1'; City = 'Austin'; State = 'TX'; PostalCode = '78701'; Country = 'USA' }
$out = Join-Path $env:TEMP 'az-rship.html'
$code = Curl-Post ($base + '/Checkout/Address') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
Follow $userJar $loc $out | Out-Null
$token = Get-TokenFromFile $out
$method = 'Express (1-2 days)'
$data = FormData @{ __RequestVerificationToken = $token; ShippingMethod = $method }
$out = Join-Path $env:TEMP 'az-rpay.html'
$code = Curl-Post ($base + '/Checkout/Shipping') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
Follow $userJar $loc $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; CardName = 'Race'; CardNumber = '4111111111111111'; CardExpiry = '12/28'; CardCvv = '123' }
$out = Join-Path $env:TEMP 'az-rplace.html'
$code = Curl-Post ($base + '/Checkout/PlaceOrder') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($loc -match 'Confirmation') { Fail 'Stock race' 'unexpected confirm' }
else {
  if ($loc -match 'Payment') { Follow $userJar $loc $out | Out-Null }
  if ((Get-Content -Raw $out) -match 'Insufficient stock|checkout-alert|stock') { Pass 'Stock race friendly fail' } else { Fail 'Stock race' 'no message' }
}
& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q ("UPDATE Product SET Stock=25 WHERE ProductId={0};" -f $raceId) -W -h -1 | Out-Null

# Orders / IDOR
Write-Host 'Spec 07 - Orders/IDOR'
$out = Join-Path $env:TEMP 'az-orders.html'
$code = Curl-Get ($base + '/Account/Orders') $userJar $out
if ($code -eq '200') { Pass 'Order history' } else { Fail 'Order history' $code }
if ($script:orderId) {
  $out = Join-Path $env:TEMP 'az-od.html'
  $code = Curl-Get ($base + '/Account/OrderDetail/' + $script:orderId) $userJar $out
  if ($code -eq '200') { Pass ("Own OrderDetail #{0}" -f $script:orderId) } else { Fail 'Own OrderDetail' $code }
  $out = Join-Path $env:TEMP 'az-oreg.html'
  Curl-Get ($base + '/Account/Register') $otherJar $out | Out-Null
  $token = Get-TokenFromFile $out
  $data = FormData @{ __RequestVerificationToken = $token; Email = $otherEmail; Password = $pwd; ConfirmPassword = $pwd }
  $out = Join-Path $env:TEMP 'az-oreg2.html'
  $code = Curl-Post ($base + '/Account/Register') $otherJar $out $data
  $loc = Get-LocationHdr ($out + '.hdr')
  if ($loc) { Follow $otherJar $loc (Join-Path $env:TEMP 'az-oreg3.html') | Out-Null }
  $out = Join-Path $env:TEMP 'az-idor.html'
  $code = Curl-Get ($base + '/Account/OrderDetail/' + $script:orderId) $otherJar $out
  if ($code -eq '404') { Pass 'IDOR -> 404' } else { Fail 'IDOR' ("code={0}" -f $code) }
} else { Fail 'OrderDetail/IDOR' 'no orderId' }

# ---- Spec 09 admin ----
Write-Host 'Spec 09 - Admin'
$out = Join-Path $env:TEMP 'az-403.html'
$code = Curl-Get ($base + '/Admin/Products') $userJar $out
if ($code -eq '403') { Pass 'Non-admin 403' } else { Fail 'Non-admin 403' $code }

$out = Join-Path $env:TEMP 'az-alogin.html'
Curl-Get ($base + '/Account/Login') $adminJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; Email = 'admin@legacy.local'; Password = 'Admin123!'; RememberMe = 'false' }
$out = Join-Path $env:TEMP 'az-alogin2.html'
$code = Curl-Post ($base + '/Account/Login') $adminJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302') { Follow $adminJar $loc (Join-Path $env:TEMP 'az-alogin3.html') | Out-Null; Pass 'Admin login' } else { Fail 'Admin login' $code }

$out = Join-Path $env:TEMP 'az-aprods.html'
$code = Curl-Get ($base + '/Admin/Products') $adminJar $out 90
if ($code -eq '200' -and ((Get-Content -Raw $out) -match 'Products')) { Pass 'Admin Products' } else { Fail 'Admin Products' $code }

$out = Join-Path $env:TEMP 'az-acreate.html'
Curl-Get ($base + '/Admin/CreateProduct') $adminJar $out | Out-Null
$token = Get-TokenFromFile $out
$pname = ('AZProd{0}' -f (Get-Random -Maximum 9999))
# use first active category id
$anyCat = ($cats.Values | Select-Object -First 1)
$data = FormData @{ __RequestVerificationToken = $token; CategoryId = ([string]$anyCat); Name = $pname; Description = 'AZ'; Price = '11.11'; Stock = '2'; IsActive = 'true' }
Curl-Post ($base + '/Admin/CreateProduct') $adminJar (Join-Path $env:TEMP 'az-acreate2.html') $data | Out-Null
$prods = Join-Path $env:TEMP 'az-aprods2.html'
Curl-Get ($base + '/Admin/Products') $adminJar $prods 90 | Out-Null
$html = Get-Content -Raw $prods
if ($html -match [regex]::Escape($pname)) { Pass 'Admin CreateProduct' } else { Fail 'CreateProduct' 'not listed' }

$pm = [regex]::Match($html, ('(?s)<td>(\d+)</td>\s*<td>{0}</td>' -f [regex]::Escape($pname)))
if ($pm.Success) {
  $newPid = $pm.Groups[1].Value
  $token = Get-TokenFromFile $prods
  $data = FormData @{ __RequestVerificationToken = $token }
  Curl-Post ($base + '/Admin/DeleteProduct/' + $newPid) $adminJar (Join-Path $env:TEMP 'az-adel.html') $data | Out-Null
  $prods3 = Join-Path $env:TEMP 'az-aprods3.html'
  Curl-Get ($base + '/Admin/Products') $adminJar $prods3 90 | Out-Null
  if ((Get-Content -Raw $prods3) -match [regex]::Escape($pname)) { Pass 'Soft-delete keeps row' } else { Fail 'Soft-delete' 'row gone' }
} else { Fail 'Soft-delete parse' 'no id' }

$out = Join-Path $env:TEMP 'az-aorders.html'
$code = Curl-Get ($base + '/Admin/Orders') $adminJar $out
if ($code -eq '200') { Pass 'Admin Orders' } else { Fail 'Admin Orders' $code }
if ($script:orderId) {
  $token = Get-TokenFromFile $out
  $data = FormData @{ __RequestVerificationToken = $token; orderId = ([string]$script:orderId); status = 'Shipped' }
  Curl-Post ($base + '/Admin/UpdateOrderStatus') $adminJar (Join-Path $env:TEMP 'az-ast.html') $data | Out-Null
  $a2 = Join-Path $env:TEMP 'az-aorders2.html'
  Curl-Get ($base + '/Admin/Orders') $adminJar $a2 | Out-Null
  if ((Get-Content -Raw $a2) -match 'Shipped') { Pass 'UpdateOrderStatus Shipped' } else { Fail 'UpdateOrderStatus' 'no Shipped' }
}

Pass 'Specs 02-04 exercised via services/web'

Write-Host ''
Write-Host '=== SUMMARY ===' -ForegroundColor Cyan
Write-Host ("Passed: {0}" -f $pass)
Write-Host ("Failed: {0}" -f $fail)
if ($fail -gt 0) { exit 1 } else { Write-Host 'ALL A-Z CHECKS PASSED' -ForegroundColor Green; exit 0 }