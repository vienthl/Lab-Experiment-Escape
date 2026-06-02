# Lab Experience Escape

**A Top-Down 2D Sci-Fi Escape Puzzle Game**

## Giới thiệu

**Lab Experience Escape** là game phiêu lưu giải đố góc nhìn từ trên xuống (top-down) lấy bối cảnh năm 2026.

Bạn vào vai **Dr. Alex Rivera** - một nhà khoa học trẻ đang làm việc tại **Facility-07**, phòng thí nghiệm bí mật của tập đoàn Helix Corp. Trong thí nghiệm "Project Elysium", việc kết hợp Neural Link với chất lạ **Nexus Fluid** đã thất bại thảm họa. Chất lỏng rò rỉ khắp nơi, tạo ra những thực thể ký ức sống gọi là **Echo Entities**, kích hoạt hệ thống lockdown toàn bộ cơ sở.

Bạn là **người sống sót cuối cùng**.  

Hãy khám phá 3 khu vực nguy hiểm ngày càng sâu, thu thập manh mối, giải quyết câu đố, nâng cấp bản thân và tìm đường thoát ra trước khi hoàn toàn bị biến đổi bởi Nexus Fluid.

## Tính năng nổi bật

- 3 Levels với độ khó tăng dần:
  - Level 1: Containment Wing
  - Level 2: Security Nexus  
  - Level 3: Core Abyss
- Hệ thống **Dual Ending** (Good Ending & Bad Ending) dựa trên số lượng Nexus Fluid thu thập
- Nhân vật thay đổi ngoại hình khi bị nhiễm độc (Appearance Change)
- Hệ thống Upgrade (HP, Speed, Shield)
- Audio Logs thu thập để khám phá câu chuyện
- Hệ thống Save/Load, Score, Sound và Animation mượt mà

## Cách chơi

- **Di chuyển**: WASD
- **Tương tác / Nhặt vật phẩm**: E
- **Dash né tránh**: Space
- **Menu tạm dừng**: ESC

## Công nghệ sử dụng

- **Game Engine**: Unity 6 (2D)
- **Ngôn ngữ lập trình**: C#
- **Công cụ chính**: Tilemap, Animator, Input System, Scene Management, PlayerPrefs Save System

## Cấu trúc thư mục dự án
Lab-Experiment-Escape/
├── Assets/
│   ├── Sprites/          # Nhân vật, Enemy, Items
│   ├── Tilesets/         # Background, Wall, Floor
│   ├── Animations/
│   ├── Audio/            # BGM, SFX
│   └── Prefabs/
├── Scripts/              # Toàn bộ code C#
├── Scenes/               # MainMenu, Level1, Level2, Level3, EndScreen
├── Docs/                 # Báo cáo, GDD, thiết kế
└── README.md

## Hướng phát triển tiếp theo

- Hoàn thiện Boss **NEXUS-CORE** (Level 3) với multi-phase
- Thêm nhiều câu đố và tương tác môi trường
- Cải thiện AI Enemy và hiệu ứng hình ảnh
- Hoàn thiện hệ thống âm thanh
- Build phiên bản cuối cùng (.exe)

## Thành viên nhóm

- **Trương Huỳnh Long Viên** (DE180530) — Leader & Level Designer
- **Huỳnh Văn Anh Huy** (DE180746) — Player & Item Developer
- **Phan Anh Vũ** (DE170544) — Enemy & NPC Developer
- **Trương Trung Hiếu** (DE180324) — UI/UX & Systems Developer
