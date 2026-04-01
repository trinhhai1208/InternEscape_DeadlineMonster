# ROAD MAP DETAILS

---

# Tuần 1

<aside>
1️⃣

Day 1:

- [x]  Setup Unity 2022.3 LTS + URP, tạo folder structure
- [x]  Import asset nhân vật FBX(Chưa làm), fix material URP (lỗi hồng)
- [x]  Viết PlayerController: tự chạy forward + đổi lane trái/phải
- [x]  Test keyboard A/D hoặc ←/→
</aside>

<aside>
2️⃣

Day 2:

- [x]  Viết BotController: chạy theo path, speed tăng dần
- [x]  Đặt bot phía sau player, test bot đuổi theo
- [x]  CheckLose theo trục Z khi bot bắt kịp
</aside>

<aside>
3️⃣

Day 3:

- [x]  Gắn Tag "Player" và "Bot" cho đúng object
- [x]  Fix OnCollisionEnter - kiểm tra Is Trigger đúng chưa
- [x]  Đặt trigger box cuối path → WIN khi player chạm
- [x]  Test đủ 2 điều kiện: WIN (về đích) + LOSE (Bot bắt kịp/Bot về đích trước)
</aside>

<aside>
4️⃣

Day 4:

- [ ]  Tạo prefab Code Commit (hình cầu nhỏ màu vàng)
- [ ]  Đặt thủ công vài cái trên path để test
- [ ]  Player chạm → +10 điểm, hiện score trên UI Text đơn giản
- [ ]  Viết ItemManager.cs cơ bản
</aside>

<aside>
5️⃣

Day 5:

- [x]  Tạo obstacle đơn giản (cube chặn lane)
- [x]  Player chạm obstacle → giảm tốc 2 giây (speed * 0.5)
- [x]  Fix camera Cinemachine: Follow + LookAt đúng player
- [x]  Review toàn bộ Tuần 1 — đảm bảo core loop chạy mượt
</aside>

---

# Tuần 2

<aside>
1️⃣

Day 1:

- [x]  Code Commit: +10 điểm, particle vàng
- [x]  Coffee: chạm → speed * 1.5 trong 1.5 giây
- [x]  Skin Up: unlock skin, speed * 1.5 đến cuối game
- [ ]  Object Pooling cho Item (tránh lag Instantiate/Destroy)
</aside>

<aside>
2️⃣

Day 2:

- [ ]  Tạo Email Bug enemy: spawn trên path
- [ ]  Email Bug bắn projectile ngược chiều player
- [ ]  Trúng projectile → -15 commit
- [ ]  Object Pooling cho Enemy + Projectile
</aside>

<aside>
3️⃣

Day 3:

- [ ]  Map 1: Văn phòng — obstacle bàn, ghế, máy tính
- [ ]  Map 2: Làng quê — obstacle đá, ao, cây
- [ ]  Map 3: Thành phố công nghệ — drone, server rack
- [ ]  Viết LoadMap.cs: chuyển scene giữa các map
</aside>

<aside>
4️⃣

Day 4:

- [ ]  Viết ObjectPool.cs dùng chung cho tất cả object
- [ ]  Viết GameManager.cs: quản lý state (Playing/Win/Lose)
- [ ]  GameManager gọi TriggerWin() / TriggerLose()
- [ ]  Dừng game khi win/lose (Time.timeScale = 0)
</aside>

<aside>
5️⃣

Day 5:

- [ ]  Menu chính: nút Play, chọn Map
- [ ]  In-game UI: score, khoảng cách bot
- [ ]  Win screen: "Promotion!" + bảng điểm
- [ ]  Lose screen: "Bug Crash!" hài hước
</aside>

---

# Tuần 3

<aside>
1️⃣

Day 1:

- [ ]  Thu thập 5 Code Commit liên tiếp → kích hoạt Flow State
- [ ]  Speed * 2 + particle ánh sáng xanh dương
- [ ]  Kéo dài 5 giây hoặc đến khi chạm obstacle
- [ ]  Viết FlowStateManager.cs
</aside>

<aside>
2️⃣

Day 2:

- [ ]  Particle khi nhặt item (vàng cho Commit, xanh cho Coffee)
- [ ]  Particle khi va chạm obstacle (đỏ)
- [ ]  Hiệu ứng Flow State: ánh sáng xanh bao quanh player
- [ ]  Import Kenney Particle Pack
</aside>

<aside>
3️⃣

Day 3:

- [ ]  Viết AudioManager.cs (singleton)
- [ ]  Footstep, coin pickup, speed boost, crash sound
- [ ]  Nhạc nền + đổi beat khi Flow State
- [ ]  Import Kenney Sound Pack
</aside>

<aside>
4️⃣

Day 4:

- [ ]  4 skin: Intern → Fresher → Junior → Senior Dev
- [ ]  Unlock skin khi nhặt Skin Up item
- [ ]  High score lưu PlayerPrefs
- [ ]  Hiện high score trên màn hình kết thúc
</aside>

<aside>
5️⃣

Day 5:

- [ ]  Build APK (Android) — test trên điện thoại thật
- [ ]  Build WebGL — test trên trình duyệt
- [ ]  Fix bug phát sinh khi build
- [ ]  Quay video demo hoặc GIF gameplay nộp mentor
</aside>