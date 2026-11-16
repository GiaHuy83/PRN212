# 👗 V-Fashion Clothing Store & AI RAG Assistant (PRN212)

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0%20(WPF)-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-3--Tier%20(DAL--BLL--UI)-orange?style=flat)](https://learn.microsoft.com/en-us/dotnet/architecture/)
[![Database](https://img.shields.io/badge/Database-Supabase%20PostgreSQL-3ECF8E?style=flat&logo=supabase&logoColor=white)](https://supabase.com/)
[![AI Providers](https://img.shields.io/badge/AI%20APIs-Gemini%20%7C%20Claude%20%7C%20OpenAI-blue?style=flat)](https://ai.google.dev/)

> **Đồ Án Mini Project Môn PRN212 (C# & .NET):** Hệ thống Quản Lý Cửa Hàng Thời Trang Quần Áo tích hợp Trợ Lý AI Chatbot RAG (Retrieval-Augmented Generation) thông minh, được xây dựng theo kiến trúc **3 Lớp Chuẩn (3-Tier Architecture)** và tách thành **2 ứng dụng WPF độc lập**.

---

## 📑 Mục Lục
1. [Giới Thiệu Dự Án](#1-giới-thiệu-dự-án)
2. [Điểm Nổi Bật Về Kỹ Thuật & AI](#2-điểm-nổi-bật-về-kỹ-thuật--ai)
3. [Kiến Trúc Hệ Thống (3-Tier)](#3-kiến-trúc-hệ-thống-3-tier)
4. [Hướng Dẫn Thiết Lập CSDL Supabase (1 Phút)](#4-hướng-dẫn-thiết-lập-csdl-supabase-1-phút)
5. [Hướng Dẫn Cài Đặt & Chạy 2 App WPF](#5-hướng-dẫn-cài-đặt--chạy-2-app-wpf)

---

## 1. Giới Thiệu Dự Án

Dự án bao gồm 2 ứng dụng WPF độc lập phục vụ nhu cầu vận hành của cửa hàng thời trang:

1. **`ClothingShop.ProductManager` (WPF App 1):** 
   - Phần mềm quản lý sản phẩm quần áo dành cho thu ngân / quản lý kho.
   - Hỗ trợ đầy đủ các thao tác **CRUD**: Xem danh sách, Thêm mới, Chỉnh sửa, Xóa sản phẩm có popup xác nhận.
   - Tìm kiếm nhanh theo từ khóa (Mã SKU, Tên, Màu sắc, Mô tả).
   - Lọc theo Danh mục (Áo sơ mi, Áo thun, Quần jean, Váy đầm, Áo khoác...) và Size (S, M, L, XL, XXL, 28-32).
   - Thống kê tự động thời gian thực: Tổng mã hàng, tổng số lượng tồn kho, tổng giá trị hàng hóa và số mã sắp hết hàng.

2. **`ClothingShop.ChatAssistant` (WPF App 2):** 
   - Trợ lý thời trang trí tuệ nhân tạo dành cho khách hàng / nhân viên tư vấn.
   - **Chế độ 1 - Chat Ra Ngoài (General LLM Chat):** Tư vấn xu hướng thời trang, mẹo phối đồ (Mix & Match), phong cách ăn mặc (Minimalism, Streetwear, Smart Casual).
   - **Chế độ 2 - Chat với AI RAG (Retrieval-Augmented Generation):** Tự động tra cứu kho sản phẩm và tài liệu bảng size chuẩn người Việt, chính sách đổi trả hàng trong 7 ngày từ **Supabase PostgreSQL** để trả lời chính xác, trung thực.
   - **Hoán chuyển linh hoạt nền tảng AI:** Hỗ trợ gọi API thật của **Google Gemini**, **Anthropic Claude**, và **OpenAI ChatGPT**.

---

## 2. Điểm Nổi Bật Về Kỹ Thuật & AI

* 🤖 **Gọi API Thật 100% (No Mock AI):** Tích hợp trực tiếp 3 bộ REST Client chính thức gọi trực tiếp tới Google Gemini (`generativelanguage.googleapis.com`), Anthropic Claude (`api.anthropic.com/v1/messages`), và OpenAI (`api.openai.com/v1/chat/completions`). Đo độ trễ mạng thực tế (Latency ms).
* 🛡️ **Router & Guardrails Bằng C# Thuần (Không Phụ Thuộc Python Laya):** Toàn bộ cơ chế bảo mật (chặn Prompt Injection, SQL Injection `DROP TABLE`, `SELECT * FROM`) và lọc câu hỏi ngoài phạm vi (thời tiết, ẩm thực, quy đổi kg/gam) được code bằng C# trong BLL, xử lý tức thì (< 1ms).
* 🧠 **RAG Pipeline Thông Minh:** Kết hợp tìm kiếm toàn văn PostgreSQL qua hàm RPC `search_knowledge_documents_text` và đối chiếu kho hàng `ProductDAO` để đưa vào prompt ngữ cảnh chống hallucination (bịa đặt thông tin).
* ❓ **Xử Lý Thiếu Dữ Liệu Lịch Sự:** Khi khách hỏi những thông tin ngoài phạm vi hoặc CSDL không có dữ liệu, AI trả lời thẳng thắn: *"Xin lỗi bạn, hiện tại trong hệ thống chưa có thông tin về câu hỏi này."*
* 🎨 **Định Dạng Markdown WPF:** Tự động chuyển đổi cú pháp Markdown (`**in đậm**`, `* gạch đầu dòng`) thành các thẻ Inlines đẹp mắt, không bị lộ dấu hoa thị thô.
* 💾 **Tự Động Lưu Cấu Hình Đa Đường Dẫn (Auto-Save):** Đồng bộ cấu hình API Key và chuỗi kết nối vào cả thư mục runtime lẫn file gốc của project, tự động ghi nhớ ngay khi gõ hoặc chuyển tab.

---

## 3. Kiến Trúc Hệ Thống (3-Tier)

```
PRN212/
│
├── ClothingShopApp.sln                     # File Solution chính
├── supabase_clothing_shop_setup.sql        # Script tạo CSDL PostgreSQL trên Supabase
├── README.md                               # Hướng dẫn chi tiết
│
├── ClothingShop.DAL/                       # TẦNG TRUY CẬP DỮ LIỆU (Data Access Layer)
│   ├── Models/
│   │   ├── ClothingProduct.cs              # Entity sản phẩm (SKU, Name, Category, Size, Price,...)
│   │   ├── KnowledgeDocument.cs            # Entity tài liệu RAG (Size guide, Chính sách đổi trả,...)
│   │   ├── AppConfig.cs                    # Cấu hình API key, Model, Supabase
│   │   ├── ChatMessage.cs                  # Entity tin nhắn, badges, thời gian phản hồi
│   │   └── RouteDecision.cs                # Metadata định tuyến Router
│   ├── DatabaseConnection.cs               # Quản lý kết nối Npgsql (hỗ trợ cả URI và Parameters)
│   ├── ProductDAO.cs                       # CRUD sản phẩm (Supabase + In-Memory Fallback)
│   ├── KnowledgeDAO.cs                     # Tra cứu RAG kiến thức (gọi RPC Supabase)
│   └── ConfigDAO.cs                        # Tự động nạp & lưu file appsettings.json đa đường dẫn
│
├── ClothingShop.BLL/                       # TẦNG NGHIỆP VỤ (Business Logic Layer)
│   ├── LLM/
│   │   ├── IAiProvider.cs                  # Interface chuẩn cho các nhà cung cấp AI
│   │   ├── GeminiProvider.cs               # Google Gemini REST Client
│   │   ├── ClaudeProvider.cs               # Anthropic Claude REST Client
│   │   └── OpenAiProvider.cs               # OpenAI ChatGPT REST Client
│   ├── ProductService.cs                   # Kiểm tra hợp lệ dữ liệu CRUD & tính toán thống kê
│   └── AiChatService.cs                    # Điều phối RAG Pipeline, Guardrails & Chat ra ngoài
│
├── ClothingShop.ProductManager/            # [APP WPF 1]: QUẢN LÝ SẢN PHẨM QUẦN ÁO (CRUD)
│   ├── ProductEditWindow.xaml(.cs)         # Cửa sổ Dialog Thêm / Sửa sản phẩm
│   ├── MainWindow.xaml(.cs)                # DataGrid, Tìm kiếm, Lọc Danh mục & Size, Thống kê
│   └── appsettings.example.json            # File mẫu cấu hình (tạo appsettings.json khi chạy)
│
└── ClothingShop.ChatAssistant/             # [APP WPF 2]: CHATBOT AI TƯ VẤN (RAG & CHAT NGOÀI)
    ├── MarkdownHelper.cs                   # Bộ phân tích Markdown thành Inlines TextBlock
    ├── Converters.cs                       # Converter hiển thị tin nhắn chat (User vs AI)
    ├── MainWindow.xaml(.cs)                # Giao diện Chat bong bóng, Cài đặt API Key thật
    └── appsettings.example.json            # File mẫu cấu hình (tạo appsettings.json khi chạy)
```

---

## 4. Hướng Dẫn Thiết Lập CSDL Supabase (1 Phút)

1. Đăng ký/Đăng nhập tài khoản miễn phí tại [https://supabase.com](https://supabase.com).
2. Tạo một Project mới (hoặc dùng project có sẵn).
3. Vào menu **SQL Editor** ở thanh điều hướng bên trái.
4. Mở file [supabase_clothing_shop_setup.sql](supabase_clothing_shop_setup.sql) trong thư mục dự án, sao chép toàn bộ nội dung và dán vào SQL Editor.
5. Nhấn **Run** (Ctrl + Enter).
   - Hệ thống sẽ tự động tạo bảng `clothing_products`, `knowledge_documents`, hàm tìm kiếm RPC `search_knowledge_documents_text` và nạp sẵn **12 sản phẩm quần áo mẫu** cùng **4 bộ tài liệu chuẩn cho AI RAG**.
6. Vào mục **Project Settings -> Database** trên Supabase, sao chép **Connection string** (dạng URI `postgresql://...` hoặc Connection Parameters).

---

## 5. Hướng Dẫn Cài Đặt & Chạy 2 App WPF

### Yêu Cầu Hệ Thống:
- Hệ điều hành: Windows 10/11.
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (hoặc Visual Studio 2022 phiên bản 17.12 trở lên với workload `.NET Desktop Development`).

### Cách 1: Chạy bằng Terminal (PowerShell / Command Prompt)

Mở 2 cửa sổ terminal riêng biệt để chạy 2 app song song:

**Cửa sổ 1 - Chạy App Quản Lý Quần Áo (CRUD):**
```powershell
dotnet run --project ClothingShop.ProductManager
```

**Cửa sổ 2 - Chạy App Trợ Lý AI Chatbot (RAG & Chat Ngoài):**
```powershell
dotnet run --project ClothingShop.ChatAssistant
```

### Cách 2: Chạy bằng Visual Studio 2022
1. Mở file `ClothingShopApp.sln`.
2. Chuột phải vào project muốn chạy (`ClothingShop.ProductManager` hoặc `ClothingShop.ChatAssistant`) -> Chọn **Set as Startup Project**.
3. Nhấn **F5** (hoặc nút Debug) để khởi động.
4. *(Mẹo chạy cả 2 app cùng lúc):* Chuột phải vào Solution -> Chọn **Properties** -> Chọn **Multiple startup projects** -> Đặt Action của cả 2 app thành **Start**.
