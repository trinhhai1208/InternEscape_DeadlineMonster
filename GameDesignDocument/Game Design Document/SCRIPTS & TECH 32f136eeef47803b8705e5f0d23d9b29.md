# SCRIPTS & TECH

# LIST SCRIPTS

- [ ]  `PlayerController.cs`
- [ ]  `BotController.cs`
- [ ]  `GameManager.cs`
- [ ]  `ItemManager.cs`
- [ ]  `ObstacleManager.cs`
- [ ]  `EnemyController.cs`
- [ ]  `UIManager.cs`
- [ ]  `ObjectPool.cs`
- [ ]  `AudioManager.cs`
- [ ]  `FlowStateManager.cs`

List error or bug:

---

# LIST COMPONENTS

Cinemachine (camera follow)

Rigidbody (physics player)

Collider (item / obstacle trigger)

Animator (run, win, crash)

---

# ASSETS

Low Poly Runner Character (itch.io)

Low Poly Office Props (itch.io)

Kenney Particle Pack

Kenney Sound Pack

Path: asset anh Nam gửi

---

# OBJECT POOLING

Thay vì Instantiate/Destroy item liên tục (gây lag), dùng Pool để tái sử dụng object. Áp dụng cho: Item, Obstacle, Enemy, Projectile. Viết 1 lần dùng cho tất cả.