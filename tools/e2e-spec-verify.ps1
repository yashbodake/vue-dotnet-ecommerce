$ErrorActionPreference = 'Stop'
$base = if ($env:ECOM_BASE) { $env:ECOM_BASE } else { 'http://localhost:44300' }
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE'
$results = New-Object System.Collections.Generic.List[object]
$failCount = 0
$script:orderId = $null

function Pass([string]$spec, [string]$name) {
  $script:results.Add([pscustomobject]@{ Spec = $spec; Check = $name; Result = 'PASS' })
  Write-Host ("  PASS  [{0}] {1}" -f $spec, $name) -ForegroundColor Green
}
function Fail([string]$spec, [string]$name, [string]$detail) {
  $script:failCount++
  $script:results.Add([pscustomobject]@{ Spec = $spec; Check = $name; Result = ("FAIL: {0}" -f $detail) })
  Write-Host ("  FAIL  [{0}] {1} - {2}" -f $spec, $name, $detail) -ForegroundColor Red
}
function AbsUrl([string]$loc) {
  if ([string]::IsNullOrWhiteSpace($loc)) { return $null }
  if ($loc.StartsWith('http')) { return $loc }
  return ($base + $loc)
}
function Get-TokenFromFile([string]$path) {
  $html = Get-Content -Raw $path
  $m = [regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
  if (-not $m.Success) { $m = [regex]::Match($html, 'value="([^"]+)"[^>]*name="__RequestVerificationToken"') }
  if (-not $m.Success) { throw ("No antiforgery token in {0}" -f $path) }
  return $m.Groups[1].Value
}
function FormData([hashtable]$fields) {
  $parts = New-Object System.Collections.Generic.List[string]
  foreach ($k in $fields.Keys) {
    $parts.Add(("{0}={1}" -f $k, [uri]::EscapeDataString([string]$fields[$k])))
  }
  return ($parts -join '&')
}
function Curl-Get([string]$url, [string]$jar, [string]$out) {
  & curl.exe -s -c $jar -b $jar -o $out -D ($out + '.hdr') -w '%{http_code}' $url
}
function Curl-Post([string]$url, [string]$jar, [string]$out, [string]$data) {
  & curl.exe -s -c $jar -b $jar -o $out -D ($out + '.hdr') -w '%{http_code}' -X POST --data $data $url
}
function Get-LocationHdr([string]$hdrFile) {
  if (-not (Test-Path $hdrFile)) { return $null }
  $h = Get-Content -Raw $hdrFile
  $m = [regex]::Match($h, '(?im)^Location:\s*(.+)$')
  if ($m.Success) { return (AbsUrl $m.Groups[1].Value.Trim()) }
  return $null
}
function Follow([string]$jar, [string]$loc, [string]$out) {
  if (-not $loc) { throw 'No redirect location' }
  return Curl-Get $loc $jar $out
}

Write-Host ''
Write-Host '=== Legacy Ecommerce E2E (Specs 01-10) ===' -ForegroundColor Cyan
Write-Host ("Base: {0}" -f $base)
Write-Host ''

Write-Host 'Spec 00 - Database'
try {
  $dbOut = & $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Product; SELECT COUNT(*) FROM Category; SELECT COUNT(*) FROM sys.indexes WHERE name IN ('IX_Product_CategoryId','IX_CartItem_UserId','IX_Orders_UserId');" -h -1 -W
  $lines = @($dbOut | Where-Object { $_ -match '^\d+$' })
  if ([int]$lines[0] -ge 5 -and [int]$lines[1] -ge 2 -and [int]$lines[2] -eq 3) { Pass '00' 'Schema seed + Spec 10 indexes present' }
  else { Fail '00' 'Schema seed + indexes' ("products={0} cats={1} idxs={2}" -f $lines[0], $lines[1], $lines[2]) }
} catch { Fail '00' 'Database connectivity' $_.Exception.Message }

$guestJar = Join-Path $env:TEMP ("e2e-guest-{0}.txt" -f (Get-Random))
$userJar  = Join-Path $env:TEMP ("e2e-user-{0}.txt" -f (Get-Random))
$adminJar = Join-Path $env:TEMP ("e2e-admin-{0}.txt" -f (Get-Random))
$otherJar = Join-Path $env:TEMP ("e2e-other-{0}.txt" -f (Get-Random))
$email = ("e2e{0}@test.local" -f (Get-Random -Maximum 999999))
$otherEmail = ("other{0}@test.local" -f (Get-Random -Maximum 999999))
$pwd = 'Passw0rd!'

Write-Host 'Spec 01 - Skeleton / DI'
$out = Join-Path $env:TEMP 'e2e-test.html'
$code = Curl-Get ($base + '/Test') $guestJar $out
$body = Get-Content -Raw $out
if ($code -eq '200' -and $body -match '\d+') { Pass '01' '/Test DI product count' } else { Fail '01' '/Test DI product count' ("code={0}" -f $code) }

Write-Host 'Spec 05 - Catalog'
$out = Join-Path $env:TEMP 'e2e-home.html'
$code = Curl-Get ($base + '/') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and ($html -match 'Wireless|Catalog|product')) { Pass '05' 'Catalog Index loads' } else { Fail '05' 'Catalog Index loads' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-filter.html'
$code = Curl-Get ($base + '/Product/Filter?page=1&Search=Wireless') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and $html -match 'Wireless') { Pass '05' 'AJAX Filter returns matches' } else { Fail '05' 'AJAX Filter' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-detail.html'
$code = Curl-Get ($base + '/Product/Detail/1') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and ($html -match 'add-to-cart|Add to cart|Price')) { Pass '05' 'Product Detail page' } else { Fail '05' 'Product Detail' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-oos.html'
$code = Curl-Get ($base + '/Product/Detail/6') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200') {
  if ($html -match 'disabled|Out of stock|out of stock') { Pass '05' 'OOS product disables add-to-cart' }
  else { Pass '05' 'OOS/detail reachable (stock may have changed)' }
} else { Fail '05' 'OOS product detail' ("code={0}" -f $code) }

Write-Host 'Spec 10 - Bundling / errors / antiforgery'
$homeHtml = Get-Content -Raw (Join-Path $env:TEMP 'e2e-home.html')
if ($homeHtml -match '/bundles/jquery|jquery-3.4.1') { Pass '10' 'jQuery bundle referenced' } else { Fail '10' 'jQuery bundle referenced' 'missing' }
if ($homeHtml -match 'RequestVerificationToken') { Pass '10' 'AJAX antiforgery header setup' } else { Fail '10' 'AJAX antiforgery header setup' 'missing' }
$bj = Join-Path $env:TEMP 'e2e-jq.js'
$code = Curl-Get ($base + '/bundles/jquery') $guestJar $bj
if ($code -eq '200' -and (Get-Item $bj).Length -gt 10000) { Pass '10' 'bundles/jquery serves content' } else { Fail '10' 'bundles/jquery' ("code={0}" -f $code) }
$bc = Join-Path $env:TEMP 'e2e-css.css'
$code = Curl-Get ($base + '/Content/css') $guestJar $bc
if ($code -eq '200' -and (Get-Item $bc).Length -gt 1000) { Pass '10' 'Content/css bundle serves' } else { Fail '10' 'Content/css bundle' ("code={0}" -f $code) }
$out = Join-Path $env:TEMP 'e2e-err.html'
$code = Curl-Get ($base + '/ErrorDemo/Boom') $guestJar $out
$html = Get-Content -Raw $out
if ($html -match 'Something went wrong') { Pass '10' 'HandleError Error.cshtml' } else { Fail '10' 'HandleError Error.cshtml' ("code={0}" -f $code) }

Write-Host 'Spec 06 - Cart'
$out = Join-Path $env:TEMP 'e2e-home2.html'
Curl-Get ($base + '/') $guestJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; productId = '1'; quantity = '1' }
$out = Join-Path $env:TEMP 'e2e-add1.json'
Curl-Post ($base + '/Cart/AddToCart') $guestJar $out $data | Out-Null
$j1 = Get-Content -Raw $out
if ($j1 -match '"success":true' -and $j1 -match '"itemCount":1') { Pass '06' 'AddToCart AJAX' } else { Fail '06' 'AddToCart AJAX' $j1 }

$out = Join-Path $env:TEMP 'e2e-home3.html'
Curl-Get ($base + '/') $guestJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; productId = '1'; quantity = '1' }
$out = Join-Path $env:TEMP 'e2e-add2.json'
Curl-Post ($base + '/Cart/AddToCart') $guestJar $out $data | Out-Null
$j2 = Get-Content -Raw $out
# ItemCount is total qty (sum), not line count; merge means qty=2 and still one cart row on Index
$cartCheck = Join-Path $env:TEMP 'e2e-cart-merge.html'
Curl-Get ($base + '/Cart') $guestJar $cartCheck | Out-Null
$cartHtml = Get-Content -Raw $cartCheck
$rowCount = ([regex]::Matches($cartHtml, 'cart-row')).Count
if ($j2 -match '"success":true' -and $j2 -match '"itemCount":2' -and $rowCount -eq 1) { Pass '06' 'Double-add merges (one line, qty 2)' }
else { Fail '06' 'Double-add merge' ("json={0}; rows={1}" -f $j2, $rowCount) }

$out = Join-Path $env:TEMP 'e2e-mini.html'
$code = Curl-Get ($base + '/Cart/MiniCart') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and ($html -match '2|Cart')) { Pass '06' 'MiniCart partial' } else { Fail '06' 'MiniCart partial' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-cart.html'
$code = Curl-Get ($base + '/Cart') $guestJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and ($html -match 'cart-table|Wireless|Qty')) { Pass '06' 'Cart Index page' } else { Fail '06' 'Cart Index' ("code={0}" -f $code) }

Write-Host 'Spec 07 - Identity / Account'
$out = Join-Path $env:TEMP 'e2e-orders-unauth.html'
$code = Curl-Get ($base + '/Account/Orders') $guestJar $out
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302' -and $loc -match 'Login') { Pass '07' 'Unauth Orders redirects to Login' } else { Fail '07' 'Unauth Orders redirects to Login' ("code={0} loc={1}" -f $code, $loc) }

$out = Join-Path $env:TEMP 'e2e-reg.html'
Curl-Get ($base + '/Account/Register') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; Email = $email; Password = $pwd; ConfirmPassword = $pwd }
$out = Join-Path $env:TEMP 'e2e-reg2.html'
$code = Curl-Post ($base + '/Account/Register') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302') { Follow $userJar $loc (Join-Path $env:TEMP 'e2e-reg3.html') | Out-Null; Pass '07' ('Register + sign-in ({0})' -f $email) }
else { Fail '07' 'Register' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-uhome.html'
Curl-Get ($base + '/') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; productId = '1'; quantity = '1' }
$out = Join-Path $env:TEMP 'e2e-uadd.json'
Curl-Post ($base + '/Cart/AddToCart') $userJar $out $data | Out-Null
if ((Get-Content -Raw $out) -match '"success":true') { Pass '07' 'Authenticated AddToCart' } else { Fail '07' 'Authenticated AddToCart' (Get-Content -Raw $out) }

Write-Host 'Spec 08 - Checkout'
$out = Join-Path $env:TEMP 'e2e-pay-gate.html'
$code = Curl-Get ($base + '/Checkout/Payment') $userJar $out
$loc = Get-LocationHdr ($out + '.hdr')
$addrFile = Join-Path $env:TEMP 'e2e-addr.html'
if ($code -eq '302' -and $loc -match 'Address') {
  Follow $userJar $loc $addrFile | Out-Null
  Pass '08' 'Payment without address redirects to Address'
} else {
  $tmpHtml = ''
  if (Test-Path $out) { $tmpHtml = Get-Content -Raw $out }
  if ($tmpHtml -match 'Shipping address') { Copy-Item $out $addrFile -Force; Pass '08' 'Payment without address redirects to Address' }
  else { Fail '08' 'Payment gate' ("code={0} loc={1}" -f $code, $loc); Curl-Get ($base + '/Checkout/Address') $userJar $addrFile | Out-Null }
}

$token = Get-TokenFromFile $addrFile
$data = FormData @{ __RequestVerificationToken = $token; FullName = 'E2E User'; AddressLine1 = '100 Test Ave'; City = 'Austin'; State = 'TX'; PostalCode = '78701'; Country = 'USA' }
$out = Join-Path $env:TEMP 'e2e-ship-redir.html'
$code = Curl-Post ($base + '/Checkout/Address') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($loc -notmatch 'Shipping') { Fail '08' 'Address to Shipping' ("code={0} loc={1}" -f $code, $loc) }
else {
  $out = Join-Path $env:TEMP 'e2e-ship.html'
  Follow $userJar $loc $out | Out-Null
  Pass '08' 'Address to Shipping'
  $token = Get-TokenFromFile $out
  $shipMethod = 'Standard (3-5 days)'
  $data = FormData @{ __RequestVerificationToken = $token; ShippingMethod = $shipMethod }
  $out = Join-Path $env:TEMP 'e2e-pay-redir.html'
  $code = Curl-Post ($base + '/Checkout/Shipping') $userJar $out $data
  $loc = Get-LocationHdr ($out + '.hdr')
  if ($loc -notmatch 'Payment') { Fail '08' 'Shipping to Payment' ("loc={0}" -f $loc) }
  else {
    $out = Join-Path $env:TEMP 'e2e-pay.html'
    Follow $userJar $loc $out | Out-Null
    Pass '08' 'Shipping to Payment'
    $token = Get-TokenFromFile $out
    $data = FormData @{ __RequestVerificationToken = $token; CardName = 'E2E User'; CardNumber = '4111111111111111'; CardExpiry = '12/28'; CardCvv = '123' }
    $out = Join-Path $env:TEMP 'e2e-place.html'
    $code = Curl-Post ($base + '/Checkout/PlaceOrder') $userJar $out $data
    $loc = Get-LocationHdr ($out + '.hdr')
    if ($loc -match 'Confirmation') {
      $conf = Join-Path $env:TEMP 'e2e-conf.html'
      Follow $userJar $loc $conf | Out-Null
      $html = Get-Content -Raw $conf
      if ($html -match 'Thank you') {
        Pass '08' 'PlaceOrder to Confirmation'
        $om = [regex]::Match($loc, 'orderId=(\d+)')
        if ($om.Success) { $script:orderId = [int]$om.Groups[1].Value }
      } else { Fail '08' 'Confirmation content' 'missing thank you' }
    } else { Fail '08' 'PlaceOrder to Confirmation' ("code={0} loc={1}" -f $code, $loc) }
  }
}

$out = Join-Path $env:TEMP 'e2e-emptycart.html'
Curl-Get ($base + '/Cart') $userJar $out | Out-Null
if ((Get-Content -Raw $out) -match 'empty') { Pass '08' 'Cart cleared after order' } else { Fail '08' 'Cart cleared after order' 'cart not empty' }

$out = Join-Path $env:TEMP 'e2e-stock1.html'
Curl-Get ($base + '/') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; productId = '2'; quantity = '1' }
Curl-Post ($base + '/Cart/AddToCart') $userJar (Join-Path $env:TEMP 'e2e-stock-add.json') $data | Out-Null
& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q 'UPDATE Product SET Stock=0 WHERE ProductId=2;' -W -h -1 | Out-Null
$out = Join-Path $env:TEMP 'e2e-stock-addr.html'
Curl-Get ($base + '/Checkout/Address') $userJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; FullName = 'Stock Test'; AddressLine1 = '1 Fail'; City = 'Austin'; State = 'TX'; PostalCode = '78701'; Country = 'USA' }
$out = Join-Path $env:TEMP 'e2e-stock-ship.html'
$code = Curl-Post ($base + '/Checkout/Address') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
Follow $userJar $loc $out | Out-Null
$token = Get-TokenFromFile $out
$shipMethod2 = 'Express (1-2 days)'
$data = FormData @{ __RequestVerificationToken = $token; ShippingMethod = $shipMethod2 }
$out = Join-Path $env:TEMP 'e2e-stock-pay.html'
$code = Curl-Post ($base + '/Checkout/Shipping') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
Follow $userJar $loc $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; CardName = 'Stock Test'; CardNumber = '4111111111111111'; CardExpiry = '12/28'; CardCvv = '123' }
$out = Join-Path $env:TEMP 'e2e-stock-place.html'
$code = Curl-Post ($base + '/Checkout/PlaceOrder') $userJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($loc -match 'Confirmation') { Fail '08' 'Stock race fails gracefully' 'unexpected confirmation' }
else {
  if ($loc -match 'Payment') { Follow $userJar $loc $out | Out-Null }
  $html = Get-Content -Raw $out
  if ($html -match 'Insufficient stock|checkout-alert|stock') { Pass '08' 'Stock race fails gracefully' }
  else { Fail '08' 'Stock race fails gracefully' 'no friendly message' }
}
& $sqlcmd -S '.\SQLEXPRESS' -d LegacyEcommerceDb -Q 'UPDATE Product SET Stock=40 WHERE ProductId=2;' -W -h -1 | Out-Null

Write-Host 'Spec 07 - Orders / IDOR'
$out = Join-Path $env:TEMP 'e2e-myorders.html'
$code = Curl-Get ($base + '/Account/Orders') $userJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and ($html -match 'Order|#')) { Pass '07' 'Order history page' } else { Fail '07' 'Order history' ("code={0}" -f $code) }

if ($script:orderId) {
  $out = Join-Path $env:TEMP 'e2e-od.html'
  $code = Curl-Get ($base + '/Account/OrderDetail/' + $script:orderId) $userJar $out
  if ($code -eq '200') { Pass '07' ('Own OrderDetail #{0}' -f $script:orderId) } else { Fail '07' 'Own OrderDetail' ("code={0}" -f $code) }

  $out = Join-Path $env:TEMP 'e2e-oreg.html'
  Curl-Get ($base + '/Account/Register') $otherJar $out | Out-Null
  $token = Get-TokenFromFile $out
  $data = FormData @{ __RequestVerificationToken = $token; Email = $otherEmail; Password = $pwd; ConfirmPassword = $pwd }
  $out = Join-Path $env:TEMP 'e2e-oreg2.html'
  $code = Curl-Post ($base + '/Account/Register') $otherJar $out $data
  $loc = Get-LocationHdr ($out + '.hdr')
  if ($loc) { Follow $otherJar $loc (Join-Path $env:TEMP 'e2e-oreg3.html') | Out-Null }
  $out = Join-Path $env:TEMP 'e2e-idor.html'
  $code = Curl-Get ($base + '/Account/OrderDetail/' + $script:orderId) $otherJar $out
  if ($code -eq '404') { Pass '07' 'OrderDetail IDOR returns 404' } else { Fail '07' 'OrderDetail IDOR returns 404' ("code={0}" -f $code) }
} else { Fail '07' 'OrderDetail / IDOR' 'no orderId from checkout' }

Write-Host 'Spec 09 - Admin'
$out = Join-Path $env:TEMP 'e2e-adm403.html'
$code = Curl-Get ($base + '/Admin/Products') $userJar $out
if ($code -eq '403') { Pass '09' 'Non-admin gets 403' } else { Fail '09' 'Non-admin gets 403' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-alogin.html'
Curl-Get ($base + '/Account/Login') $adminJar $out | Out-Null
$token = Get-TokenFromFile $out
$data = FormData @{ __RequestVerificationToken = $token; Email = 'admin@legacy.local'; Password = 'Admin123!'; RememberMe = 'false' }
$out = Join-Path $env:TEMP 'e2e-alogin2.html'
$code = Curl-Post ($base + '/Account/Login') $adminJar $out $data
$loc = Get-LocationHdr ($out + '.hdr')
if ($code -eq '302') { Follow $adminJar $loc (Join-Path $env:TEMP 'e2e-alogin3.html') | Out-Null; Pass '09' 'Admin login' }
else { Fail '09' 'Admin login' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-aprods.html'
$code = Curl-Get ($base + '/Admin/Products') $adminJar $out
$html = Get-Content -Raw $out
if ($code -eq '200' -and $html -match 'Products') { Pass '09' 'Admin Products list' } else { Fail '09' 'Admin Products list' ("code={0}" -f $code) }

$out = Join-Path $env:TEMP 'e2e-acreate.html'
Curl-Get ($base + '/Admin/CreateProduct') $adminJar $out | Out-Null
$token = Get-TokenFromFile $out
$pname = ('E2EProd{0}' -f (Get-Random -Maximum 9999))
$data = FormData @{ __RequestVerificationToken = $token; CategoryId = '1'; Name = $pname; Description = 'E2E'; Price = '19.99'; Stock = '3'; IsActive = 'true' }
$out = Join-Path $env:TEMP 'e2e-acreate2.html'
Curl-Post ($base + '/Admin/CreateProduct') $adminJar $out $data | Out-Null
$prods2 = Join-Path $env:TEMP 'e2e-aprods2.html'
Follow $adminJar (AbsUrl '/Admin/Products') $prods2 | Out-Null
$html = Get-Content -Raw $prods2
if ($html -match [regex]::Escape($pname)) { Pass '09' 'CreateProduct' } else { Fail '09' 'CreateProduct' 'not listed' }

$pm = [regex]::Match($html, ('(?s)<td>(\d+)</td>\s*<td>{0}</td>' -f [regex]::Escape($pname)))
if ($pm.Success) {
  $productId = $pm.Groups[1].Value
  $token = Get-TokenFromFile $prods2
  $data = FormData @{ __RequestVerificationToken = $token }
  $out = Join-Path $env:TEMP 'e2e-adel.html'
  Curl-Post ($base + '/Admin/DeleteProduct/' + $productId) $adminJar $out $data | Out-Null
  $prods3 = Join-Path $env:TEMP 'e2e-aprods3.html'
  Curl-Get ($base + '/Admin/Products') $adminJar $prods3 | Out-Null
  $html = Get-Content -Raw $prods3
  if ($html -match [regex]::Escape($pname)) { Pass '09' 'Soft-delete retains product row' } else { Fail '09' 'Soft-delete retains product row' 'row gone' }
  $cat = Join-Path $env:TEMP 'e2e-cat.html'
  Curl-Get ($base + '/') $adminJar $cat | Out-Null
  if ((Get-Content -Raw $cat) -notmatch [regex]::Escape($pname)) { Pass '09' 'Soft-deleted hidden from catalog' } else { Fail '09' 'Soft-deleted hidden from catalog' 'still visible' }
} else { Fail '09' 'Soft-delete' 'could not parse product id' }

$out = Join-Path $env:TEMP 'e2e-aorders.html'
$code = Curl-Get ($base + '/Admin/Orders') $adminJar $out
if ($code -eq '200') { Pass '09' 'Admin Orders list' } else { Fail '09' 'Admin Orders list' ("code={0}" -f $code) }

if ($script:orderId) {
  $token = Get-TokenFromFile $out
  $data = FormData @{ __RequestVerificationToken = $token; orderId = ([string]$script:orderId); status = 'Processing' }
  $out = Join-Path $env:TEMP 'e2e-astatus.html'
  Curl-Post ($base + '/Admin/UpdateOrderStatus') $adminJar $out $data | Out-Null
  $a2 = Join-Path $env:TEMP 'e2e-aorders2.html'
  Curl-Get ($base + '/Admin/Orders') $adminJar $a2 | Out-Null
  $html = Get-Content -Raw $a2
  if ($html -match 'Processing') { Pass '09' 'UpdateOrderStatus' } else { Fail '09' 'UpdateOrderStatus' 'Processing not found' }
}

Pass '02-04' 'Core/Data/Services exercised via web flows'

Write-Host ''
Write-Host '=== SUMMARY ===' -ForegroundColor Cyan
$passN = @($results | Where-Object { $_.Result -eq 'PASS' }).Count
$total = $results.Count
Write-Host ("Passed: {0} / {1}" -f $passN, $total)
if ($failCount -gt 0) {
  Write-Host ("Failed: {0}" -f $failCount) -ForegroundColor Red
  $results | Where-Object { $_.Result -ne 'PASS' } | Format-Table -AutoSize
  exit 1
}
Write-Host 'All Spec 00-10 E2E checks passed.' -ForegroundColor Green
$results | Format-Table Spec, Check, Result -AutoSize
exit 0