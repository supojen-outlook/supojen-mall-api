# 四表關係圖
```
promotions (1)
  │
  ├─ promotion_rules (N)    ← 一個活動可以有多條規則
  │    (滿額減、折扣、贈品、免運)
  │
  ├─ promotion_scopes (N)    ← 一個活動可以適用多個範圍
  │    (商品、類別、品牌、全館)
  │
  └─ promotion_usages (N)    ← 一個活動可以被多次使用
       (記錄誰、何時、哪個訂單使用)
```