# 🏃‍♂️ Intern Escape: Deadline Monster 🛑

**Intern Escape: Deadline Monster** là một trò chơi thể loại **Runner** kịch tính, nơi bạn vào vai một thực tập sinh đang cố gắng thoát khỏi sự truy đuổi gắt gao của "Boss(Sếp của bạn)" (con quái vật đại diện cho áp lực công việc). Thu thập kiến thức, nâng cấp bản thân và né tránh những lỗi "Bug" để chạm đích!

---

## 🎮 Tính Năng Nổi Bật

- **Hệ thống Path Generation**: Bản đồ được tạo tự động với các đoạn đường cong, dốc mượt mà.
- **Tiến hóa Nhân vật (Skin Evolution)**: Nhặt vật phẩm để nâng cấp từ *Intern* lên *Fresher*, *Junior* và *Senior*. Mỗi cấp độ tăng dần tốc độ chạy.
- **Hệ thống Item đa dạng**:
    - 📦 **CodeCommit**: Thu thập để tăng điểm số.
    - ☕ **Coffee**: Tăng tốc độ tạm thời (Boost).
    - 🐛 **Obstacles (Bug)**: Làm chậm nhân vật, khiến bạn dễ bị Deadline bắt kịp.
- **AI Deadline Monster**: Kẻ thù luôn đeo bám và tăng tốc độ theo thời gian, tạo áp lực liên tục.
- **Âm Thanh Sống Động**: Hệ thống âm thanh môi trường và tiếng bước chân 3D (âm lượng thay đổi theo khoảng cách của Boss).
- **Điều khiển đa nền tảng**: Hỗ trợ phím (PC) và vuốt (Mobile) mượt mà.

---

## 🛠 Cấu Trúc Dự Án (Kỹ Thuật)

### 📂 Thư Mục Chính
- `Assets/Scripts/Core`: GameManager, AudioManager (Quản lý trạng thái và âm thanh xuyên suốt).
- `Assets/Scripts/Player`: Điều khiển nhân vật, xử lý va chạm và tiến hóa skin.
- `Assets/Scripts/AI`: BotController điều hành Deadline Monster.
- `Assets/Scripts/World`: ItemManager, GenMap (Sinh vật phẩm và đường chạy).
- `Assets/Editor`: PathGeneratorTool (Công cụ xây dựng đường chạy trực quan).

### ⚙️ Công Nghệ Sử Dụng
- **Game Engine**: Unity 2022.3.35f1
- **Ngôn ngữ**: C#
- **Hệ thống Camera**: Cinemachine (Camera Intro Cinematic).
- **UI**: TextMeshPro & Unity UI Canvas.

---

## 🚀 Hướng Dẫn Cài Đặt (Cho Developer)

1. Clone repository này về máy:
   ```bash
   git clone [URL_REPO_CỦA_BẠN]
   ```
2. Mở dự án bằng **Unity Hub**.
3. Đảm bảo đã cài đặt các Package: `Cinemachine`, `TextMeshPro`.
4. Mở Scene `MainMenu` và ấn **Play** để bắt đầu trải nghiệm.

---

## 🕹 Cách Điều Khiển

| Hành động | PC (Keyboard) | Mobile (Touch) |
| :--- | :--- | :--- |
| **Di chuyển Trái** | phím `A` hoặc `<-` | Vuốt sang Trái |
| **Di chuyển Phải** | phím `D` hoặc `->` | Vuốt sang Phải |

---

## 📝 Giấy Phép (License)
Dự án được phát triển cho mục đích học tập và giải trí.
Nếu bạn có góp ý vui lòng liên hệ: 
 - [Facebook] https://www.facebook.com/trinh.hai.12082004
 - [Email] [trinhhai758@gmail.com]
 - [Phone] 0342915766
© 2026 **Trịnh Ngọc Hải**
