# S03 ACCEPTANCE

- [ ] Login returns JWT with role claims for admin
- [ ] Invalid password → 401
- [ ] `/api/auth/me` without token → 401; with token → user dto
- [ ] API restart does not duplicate admin user
- [ ] Auth tests pass (include negative login/me cases)
