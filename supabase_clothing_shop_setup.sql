-- ============================================================================
-- SCRIPT SUPABASE POSTGRESQL: HỆ THỐNG CỬA HÀNG THỜI TRANG & AI RAG CHATBOT
-- Môn: PRN212 - C# & WPF Mini Project
-- Chức năng: Quản lý Quần Áo (CRUD) + RAG Knowledge Base cho AI Chatbot
-- ============================================================================

-- 1. BẢNG SẢN PHẨM QUẦN ÁO (CLOTHING PRODUCTS)
CREATE TABLE IF NOT EXISTS public.clothing_products (
    id BIGSERIAL PRIMARY KEY,
    sku VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    category VARCHAR(100) NOT NULL,
    size VARCHAR(50) NOT NULL,
    color VARCHAR(50) NOT NULL,
    price NUMERIC(12, 2) NOT NULL,
    stock_quantity INT NOT NULL DEFAULT 0,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Index cho tìm kiếm nhanh
CREATE INDEX IF NOT EXISTS idx_clothing_products_sku ON public.clothing_products(sku);
CREATE INDEX IF NOT EXISTS idx_clothing_products_category ON public.clothing_products(category);
CREATE INDEX IF NOT EXISTS idx_clothing_products_name ON public.clothing_products(name);

-- 2. BẢNG TÀI LIỆU KIẾN THỨC RAG (KNOWLEDGE DOCUMENTS)
-- Dùng để cung cấp ngữ cảnh cho AI: Bảng quy đổi size, Chính sách đổi trả, Bảo quản, Khuyến mãi
CREATE TABLE IF NOT EXISTS public.knowledge_documents (
    id BIGSERIAL PRIMARY KEY,
    category VARCHAR(100) NOT NULL DEFAULT 'general',
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_knowledge_documents_category ON public.knowledge_documents(category);

-- 3. HÀM RPC TÌM KIẾM TÀI LIỆU CHO AI RAG (FULL-TEXT SEARCH POSTGRESQL)
-- Kế thừa cơ chế tìm kiếm 2 tầng tương tự như bên dự án Capstone
CREATE OR REPLACE FUNCTION public.search_knowledge_documents_text (
    query_text text,
    result_limit int DEFAULT 5
)
RETURNS TABLE (
    id bigint,
    category varchar,
    title varchar,
    content text,
    metadata jsonb
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    -- Giai đoạn 1: Khớp chuỗi chính xác (Exact phrase match)
    RETURN QUERY
    SELECT kd.id, kd.category, kd.title, kd.content, kd.metadata
    FROM public.knowledge_documents kd
    WHERE
        kd.title ILIKE '%' || query_text || '%'
        OR kd.content ILIKE '%' || query_text || '%'
        OR kd.category ILIKE '%' || query_text || '%'
    LIMIT result_limit;

    IF FOUND THEN
        RETURN;
    END IF;

    -- Giai đoạn 2: Khớp từ khóa thông minh (Keyword ranking)
    RETURN QUERY
    WITH words AS (
        SELECT unnest(string_to_array(lower(regexp_replace(query_text, '[^a-zA-Z0-9À-ỹ]+', ' ', 'g')), ' ')) AS w
    ),
    filtered_words AS (
        SELECT DISTINCT w FROM words 
        WHERE length(w) >= 3 
          AND w NOT IN ('bạn', 'cho', 'tôi', 'mình', 'của', 'các', 'những', 'được', 'không', 'nào', 'với', 'trong', 'và', 'hay', 'là', 'có', 'thế', 'nào', 'gì', 'ạ', 'xin')
    )
    SELECT kd.id, kd.category, kd.title, kd.content, kd.metadata
    FROM public.knowledge_documents kd
    WHERE EXISTS (
        SELECT 1 FROM filtered_words fw 
        WHERE kd.title ~* ('\m' || fw.w || '\M') 
           OR kd.content ~* ('\m' || fw.w || '\M')
    )
    ORDER BY (
        SELECT count(*) FROM filtered_words fw 
        WHERE kd.title ~* ('\m' || fw.w || '\M') 
           OR kd.content ~* ('\m' || fw.w || '\M')
    ) DESC
    LIMIT result_limit;
END;
$$;

-- 4. DỮ LIỆU MẪU SẢN PHẨM QUẦN ÁO (SAMPLE CLOTHING PRODUCTS)
INSERT INTO public.clothing_products (sku, name, category, size, color, price, stock_quantity, description)
VALUES
('SM-01', 'Áo Sơ Mi Nam Oxford Dài Tay', 'Áo sơ mi', 'L', 'Trắng', 350000, 45, 'Chất liệu vải Oxford 100% cotton dệt chéo, đứng form, chống nhăn nhẹ, lịch sự nơi công sở.'),
('SM-02', 'Áo Sơ Mi Nam Oxford Dài Tay', 'Áo sơ mi', 'M', 'Xanh Navy', 350000, 30, 'Vải cotton thoáng mát, form regular fit chuẩn tôn dáng quý ông.'),
('TS-01', 'Áo Thun Unisex Cotton Compact Cổ Tròn', 'Áo thun', 'XL', 'Đen', 190000, 80, 'Cotton Compact 250gsm dày dặn, thấm hút mồ hôi tốt, co giãn 4 chiều.'),
('TS-02', 'Áo Thun Unisex Cotton Compact Cổ Tròn', 'Áo thun', 'M', 'Beige', 190000, 65, 'Màu be thanh lịch, trẻ trung, phù hợp phong cách dạo phố minimalist.'),
('PL-01', 'Áo Polo Nam Pique Phối Bo Cổ Cổ Điển', 'Áo polo', 'L', 'Ghi Xám', 280000, 40, 'Vải dệt Pique gai cá sấu cao cấp, mềm mịn, bo dệt cổ áo không lo bị quăn mép.'),
('JN-01', 'Quần Jean Nam Slim-fit Wash Nhẹ Cao Cấp', 'Quần jean', '31', 'Xanh Indigo', 450000, 25, 'Vải denim co giãn 2% spandex, không cấn khi ngồi, đường may chỉ kép chắc chắn.'),
('JN-02', 'Quần Jeans Nữ Ống Suông Ống Rộng Retro', 'Quần jean', '28', 'Xanh Nhạt', 390000, 35, 'Cạp cao che khuyết điểm bụng, tôn dáng chân dài phong cách Hàn Quốc.'),
('QT-01', 'Quần Tây Âu Nam Hàn Quốc Xếp Ly Co Giãn', 'Quần âu', '32', 'Đen', 390000, 50, 'Chất vải Tuýt-si cao cấp, mềm mượt, lưng chun co giãn tiện lợi.'),
('VD-01', 'Đầm Voan Hoa Nhí Cổ Vuông Tay Phồng', 'Váy đầm', 'S', 'Hồng Pastel', 420000, 20, 'Váy hoa phong cách vintage dịu dàng, lót lụa mềm mại bên trong.'),
('CV-01', 'Chân Váy Chữ A Kaki Xếp Ly Công Sở', 'Chân váy', 'M', 'Be', 250000, 30, 'Kaki Hàn Quốc đứng form, có quần bảo hộ bên trong an toàn thoải mái.'),
('AK-01', 'Áo Khoác Gió Bomber 2 Lớp Chống Nước', 'Áo khoác', 'XL', 'Rêu Đậm', 490000, 15, 'Chống tia UV, cản gió và chống nước đi mưa nhỏ hiệu quả.'),
('BL-01', 'Áo Blazer Nữ Form Rộng Phong Cách Hàn Quốc', 'Áo blazer', 'M', 'Nâu Tây', 550000, 18, 'Form oversize hiện đại, lót vải lụa Habutai cao cấp, thích hợp đi làm và đi tiệc.')
ON CONFLICT (sku) DO NOTHING;

-- 5. DỮ LIỆU TÀI LIỆU KIẾN THỨC CHO AI RAG (KNOWLEDGE DOCUMENTS)
INSERT INTO public.knowledge_documents (category, title, content, metadata)
VALUES
(
    'size_guide',
    'Bảng Tra Cứu Size Chuẩn Người Việt Nam (Chiều Cao & Cân Nặng)',
    'HƯỚNG DẪN CHỌN SIZE QUẦN ÁO CHUẨN FORM SHOP:
- Size S: Chiều cao 1m50 - 1m60 | Cân nặng 42kg - 52kg (Vòng ngực 82-86cm, Vòng eo 64-68cm).
- Size M: Chiều cao 1m58 - 1m68 | Cân nặng 53kg - 62kg (Vòng ngực 86-92cm, Vòng eo 68-74cm). Thích hợp người 1m65 nặng 58kg.
- Size L: Chiều cao 1m66 - 1m75 | Cân nặng 63kg - 72kg (Vòng ngực 92-98cm, Vòng eo 74-82cm). Thích hợp người 1m70 nặng 65kg.
- Size XL: Chiều cao 1m72 - 1m82 | Cân nặng 73kg - 82kg (Vòng ngực 98-106cm, Vòng eo 82-90cm). Thích hợp người 1m75 nặng 78kg.
- Size XXL: Chiều cao 1m78 - 1m88 | Cân nặng 83kg - 95kg.
* LƯU Ý KHI CHỌN SIZE:
1. Nếu bạn thích mặc ôm gọn gàng hãy chọn đúng size. Nếu thích mặc form rộng thoải mái (Oversize) hãy tăng lên 1 size.
2. Với người có bụng hoặc cơ bắp to, nên ưu tiên chọn size theo cân nặng trước, sau đó chỉnh lai sau.',
    '{"tags": ["size", "chọn size", "chiều cao", "cân nặng", "áo", "quần"]}'::jsonb
),
(
    'return_policy',
    'Chính Sách Đổi Trả Hàng Trong Vòng 7 Ngày',
    'QUY ĐỊNH ĐỔI TRẢ HÀNG TẠI CỬA HÀNG THỜI TRANG:
1. Thời hạn đổi trả: Khách hàng được đổi mẫu, đổi size trong vòng 07 ngày kể từ ngày nhận hàng.
2. Điều kiện sản phẩm: Sản phẩm còn nguyên tem mác, hoá đơn mua hàng, chưa qua sử dụng, chưa giặt tẩy và không có mùi lạ (nước hoa, mồ hôi).
3. Miễn phí đổi hàng: Đổi size hoặc đổi sản phẩm tương đương hoàn toàn miễn phí nếu do lỗi sản xuất (rách chỉ, lỗi cúc, phai màu) hoặc nhân viên gửi nhầm size.
4. Đổi theo ý thích cá nhân: Khách hàng chịu phí ship 2 chiều (khoảng 30.000 VNĐ) nếu muốn đổi màu hoặc đổi mẫu sang sản phẩm khác.
5. Không áp dụng đổi trả: Các sản phẩm trong danh mục xả kho giảm giá trên 50%, quần lót, tất (vớ).',
    '{"tags": ["đổi trả", "hoàn tiền", "bảo hành", "7 ngày", "chính sách"]}'::jsonb
),
(
    'care_guide',
    'Cẩm Nang Giặt Là & Bảo Quản Trang Phục Bền Đẹp',
    'CÁCH BẢO QUẢN QUẦN ÁO KHÔNG BỊ XÙ LÔNG, PHAI MÀU:
1. Áo sơ mi & Quần Tây: Nên giặt ở chế độ giặt nhẹ (Gentle Wash), không vắt kiệt để tránh gãy nếp cổ áo. Phơi bằng móc bản to để giữ form vai áo.
2. Quần Jean / Denim: Trong lần giặt đầu tiên, nên ngâm nước muối pha loãng 2 tiếng để giữ màu vải luôn mới. Tuyệt đối không giặt chung đồ trắng với quần Jean màu đậm.
3. Áo Thun Cotton Compact & Đầm Voan: Nên lộn trái mặt vải trước khi giặt, tránh phơi trực tiếp dưới ánh nắng gay gắt ban trưa để không bị giòn sợi cotton.
4. Áo Khoác Gió Bomber: Tránh dùng chất tẩy javen mạnh, nên lau khăn ẩm hoặc giặt tay nhẹ nhàng.',
    '{"tags": ["giặt là", "bảo quản", "phơi đồ", "jean", "sơ mi"]}'::jsonb
),
(
    'vip_promotion',
    'Chính Sách Tích Điểm Thành Viên & Khuyến Mãi VIP',
    'CHƯƠNG TRÌNH KHÁCH HÀNG THÂN THIẾT SHOP:
- Khách hàng Mới: Tặng ngay voucher giảm giá 10% cho đơn hàng đầu tiên khi đăng ký tài khoản.
- Hạng Bạc (Silver - Tích luỹ trên 2.000.000đ): Giảm trực tiếp 5% cho tất cả đơn hàng, tặng voucher 100.000đ vào ngày sinh nhật.
- Hạng Vàng (Gold - Tích luỹ trên 5.000.000đ): Giảm 10% trọn đời mọi đơn hàng, miễn phí vận chuyển toàn quốc.
- Hạng Kim Cương (Diamond - Tích luỹ trên 10.000.000đ): Giảm 15% trọn đời, ưu tiên nhận thông báo bộ sưu tập mới trước 7 ngày, đổi trả linh hoạt trong 30 ngày.',
    '{"tags": ["tích điểm", "vip", "khuyến mãi", "voucher", "giảm giá", "sinh nhật"]}'::jsonb
);

-- Hoàn tất thiết lập
SELECT count(*) AS total_products FROM public.clothing_products;
SELECT count(*) AS total_documents FROM public.knowledge_documents;

