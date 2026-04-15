# 五表關係圖
```
orders (1)
  │
  ├─ order_items (N)  ←───────── returns (N)
  │    │                            (一個訂單項目可多次退貨)
  │    ├─ shipments (N)
  │    │   (一個訂單項目可分批出貨)
  │    │
  │    └─ (退貨數量追蹤：returned_quantity)
  │
  └─ payments (N)
       (一個訂單可多筆付款)
```