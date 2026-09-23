# DEEPEN-LOG — MiniTrace

- 2026-09-23 | feat: chống hàng giả — xác thực quét mã (VerifyCount/IP/GPS/SĐT) + phát hiện nghi giả khi quét nhiều nơi hoặc sau khi đã bán. Port từ InBrandCloud `Inv_InventoryVerifiedID` (doc 09 §9). Thêm entity `Verification` + enum `VerifyStatus`, `TraceService.VerifyAsync/VerificationsAsync`, API `POST /api/verify` (công khai) + `POST /api/v1/verify` + `GET /api/v1/verifications`, seed mẫu, trang SPA "Chống hàng giả". Build Release 0 error; smoke test: quét lần 3 ở địa điểm khác → status=2 "Nghi hàng giả". Commit 360dcd7, đã push origin/main.
