const API_BASE_URL = "https://localhost:5001/api";
const BACKEND_BASE_URL = "https://localhost:5001";

let products = [];
let mockVouchers = [];
let newProductsList = [];
let currentMoiIndex = 0;

// Hàm định dạng tiền tệ VND dùng chung (Đã tối ưu sửa lỗi NaN)
const formatVND = (amount) => {
    const parsedAmount = Number(amount);
    if (amount === undefined || amount === null || isNaN(parsedAmount) || parsedAmount === 0) return 'Liên hệ';
    return parsedAmount.toLocaleString('vi-VN') + 'đ';
};

// 🌟 HÀM TIỆN ÍCH BẢO VỆ GIÁ KHÔNG BỊ BIẾN THÀNH 0Đ KHI BỊ NULL/UNDEFINED/EMPTY
const validatePrice = (obj, key1, key2) => {
    const val = obj[key1] !== undefined && obj[key1] !== null && obj[key1] !== "" ? obj[key1] : obj[key2];
    const num = Number(val);
    return isNaN(num) ? 0 : num;
};

// ── 1. HÀM FETCH DATA CHUẨN HÓA CHO C# CONTROLLER ──────────────────────────
async function fetchData(endpoint) {
    try {
        const response = await fetch(`${API_BASE_URL}/${endpoint}`);
        if (!response.ok) throw new Error(`Lỗi kết nối API: ${response.status}`);

        const result = await response.json();
        console.log(`[API Log] Dữ liệu từ endpoint [${endpoint}]:`, result);

        const isSuccess = result.success !== undefined ? result.success : result.Success;
        const data = result.data !== undefined ? result.data : result.Data;

        if (isSuccess && data !== undefined) {
            return data;
        }
        return result;
    } catch (error) {
        console.error(`[API Error] Lỗi khi lấy dữ liệu từ ${endpoint}:`, error);
        return null;
    }
}

// ── 2. ĐIỀU HƯỚNG KHI TẢI TRANG ───────────────────────────────────────────
document.addEventListener("DOMContentLoaded", async () => {
    updateCartBadge();

    const path = window.location.pathname.toLowerCase();
    console.log("[Route Log] Đường dẫn trang hiện tại:", path);

    mockVouchers = await fetchData("promotion") || [];

    if (path.includes("index.html") || path === "/" || path.endsWith("/") || path === "") {
        await initHomePage();
    }
    else if (path.includes("chitietsp.html")) {
        await initDetailPage();
    }
    else if (path.includes("danhmuc.html")) {
        const productResponse = await fetchData("product?page=1&pageSize=24");
        products = productResponse ? (productResponse.items || productResponse.Items || productResponse) : [];
        initProductPages();
    }
    else if (path.includes("giohang.html")) {
        renderCart();
    }
});

// ── 3. KHỞI TẠO DỮ LIỆU CHO TRANG CHỦ ─────────────────────────────────────
async function initHomePage() {
    if (mockVouchers && mockVouchers.length > 0) {
        renderVouchers(mockVouchers);
    }

    console.log("[Logic Log] Bắt đầu nạp dữ liệu phân loại trang chủ...");

    const bestSellersData = await fetchData("product/best-sellers?limit=4");
    const newArrivalsData = await fetchData("product/new-arrivals?limit=5");
    const discountsData = await fetchData("product/discounts?limit=4");

    // 💡 HÀM ĐỂ PHÁT HIỆN CHÍNH XÁC MẢNG NẰM Ở ĐÂU TRONG LAYER TRẢ VỀ
    const extractArray = (res) => {
        if (!res) return [];
        if (Array.isArray(res)) return res; // Nếu bản thân nó đã là mảng
        if (res.items && Array.isArray(res.items)) return res.items; // Nếu nằm trong .items
        if (res.Items && Array.isArray(res.Items)) return res.Items; // Nếu nằm trong .Items
        return [];
    };

    const cleanBanChay = extractArray(bestSellersData);
    const cleanKhuyenMai = extractArray(discountsData);
    const cleanNewArrivals = extractArray(newArrivalsData);
    const allProductsData = await fetchData("product?page=1&pageSize=8");
    const cleanAllProducts = extractArray(allProductsData);

    console.log("[Debug Log] Mảng bán chạy sau bóc tách:", cleanBanChay);

    renderProducts('product-list', cleanAllProducts);
    renderProducts('list-banchay', cleanBanChay);
    renderProducts('list-khuyenmai', cleanKhuyenMai);

    if (cleanNewArrivals.length > 0) {
        newProductsList = cleanNewArrivals;

        // 💡 GIẢI PHÁP: Gọi API chi tiết của sản phẩm đầu tiên để lấy đầy đủ Description từ DB
        const firstProductId = newProductsList[currentMoiIndex].productId || newProductsList[currentMoiIndex].id || newProductsList[currentMoiIndex].Id;

        fetchData(`product/${firstProductId}`).then(fullProductData => {
            const productShow = fullProductData?.data || fullProductData?.Data || fullProductData || newProductsList[currentMoiIndex];
            renderProductShowcase('showcase-container', productShow);
        });
    } else {
        const showcase = document.getElementById('showcase-container');
        if (showcase) showcase.innerHTML = "<p style='padding:20px;'>Chưa có sản phẩm mới cập nhật.</p>";
    }

 
}

// ── 4. HÀM RENDER HTML SẢN PHẨM KHỚP TRƯỜNG DỮ LIỆU C# ────────────────────
function renderProducts(containerId, productData) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.warn(`[DOM Warn] Không tìm thấy thẻ HTML có id='${containerId}'`);
        return;
    }

    if (!productData || productData.length === 0) {
        container.innerHTML = "<p style='padding: 20px; text-align:center; width:100%; color:#888;'>Không tìm thấy sản phẩm nào.</p>";
        return;
    }

    container.innerHTML = productData.map(item => {
        const pId = item.productId || item.ProductId || item.id || item.Id;
        const pName = item.name || item.Name || "Sản phẩm không tên";

        // 🌟 Áp dụng bộ lọc sửa lỗi 0đ an toàn tuyệt đối
        const pPrice = validatePrice(item, 'price', 'Price');
        const pDiscount = validatePrice(item, 'discount', 'Discount');

        const pMainImage = item.mainImage || item.MainImage || item.imageUrl || item.ImageUrl || item.image || "";

        let fullImageUrl = "https://placehold.co/300x400?text=No+Image";
        if (pMainImage) {
            fullImageUrl = pMainImage.startsWith("http") ? pMainImage : `${BACKEND_BASE_URL}/${pMainImage}`;
        }

        const hasDiscount = pDiscount > 0;
        const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

        let discountBadgeHTML = '';
        let oldPriceHTML = '';

        if (hasDiscount) {
            discountBadgeHTML = `<span class="discount-badge" style="background-color:red; color:white; padding:2px 5px; position:absolute; z-index:2;">-${pDiscount}%</span>`;
            oldPriceHTML = `<span class="old-price" style="text-decoration: line-through; color: #999; margin-right: 5px; font-size: 13px;">${formatVND(pPrice)}</span>`;
        }

        return `
            <div class="product-card">
                <a href="chitietsp.html?id=${pId}" class="product-card-link">
                    ${discountBadgeHTML}
                    <img src="${fullImageUrl}" alt="${pName}" onerror="this.onerror=null; this.src='https://placehold.co/300x400?text=Image+Error';">
                </a>
                <div class="product-info">
                    <p class="product-name">${pName}</p>
            
                    <div class="product-controls-row">
                        <div class="price-box">
                            ${oldPriceHTML}
                            <span class="price">${formatVND(finalPrice)}</span>
                        </div>
                
                        <div class="product-bottom">
                            <div class="cart-bottom-btn" onclick="openQuickVariantModal(${pId}, 'add_to_cart')">
                                <i class="fa-solid fa-cart-plus"></i>
                            </div>
                    
                            <button class="buy-btn" onclick="openQuickVariantModal(${pId}, 'buy_now')">Mua</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function renderProductShowcase(containerId, product) {
    const container = document.getElementById(containerId);
    if (!container || !product) return;

    const pId = product.productId || product.id || product.Id;
    const pName = product.name || product.Name || "Sản phẩm không tên";

    // 🌟 Áp dụng bộ lọc sửa lỗi 0đ an toàn tuyệt đối
    const pPrice = validatePrice(product, 'price', 'Price');
    const pDiscount = validatePrice(product, 'discount', 'Discount');

    const pDesc = product.description || product.Description || "";
    const pMainImage = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || product.image || "";

    let fullImageUrl = "https://placehold.co/300x400?text=No+Image";
    if (pMainImage) {
        fullImageUrl = pMainImage.startsWith("http") ? pMainImage : `${BACKEND_BASE_URL}/${pMainImage}`;
    }

    const hasDiscount = pDiscount > 0;
    const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

    container.innerHTML = `
        <div class="product-image-section">
           <a href="chitietsp.html?id=${pId}">
                <img src="${fullImageUrl}" alt="${pName}" class="main-product-img" onerror="this.onerror=null; this.src='https://placehold.co/300x400?text=Image+Error';">
            </a>
        </div>
        <div class="product-info-section">
            <a href="chitietsp.html?id=${pId}" style="text-decoration: none; color: inherit;">
                <h3 class="product-title-highlight" style="color: #ff6b81; font-size: 26px; margin-bottom: 15px; font-weight: bold;">${pName}</h3>
            </a>
            <div class="product-description" style="margin-bottom: 20px; color: #555; line-height: 1.6; font-size: 15px;">
                <p>${pDesc}</p>
            </div>
            <div class="product-actions">
                <div style="display:flex; flex-direction:column; margin-right: 20px;">
                    ${hasDiscount ? `<span style="text-decoration: line-through; color: #999; font-size:14px; margin-bottom:4px;">${formatVND(pPrice)} (-${pDiscount}%)</span>` : ''}
                    <span class="price" style="color: red; font-size: 26px; font-weight: bold;">${formatVND(finalPrice)}</span>
                </div>
                <button class="buy-now-btn" onclick="window.location.href='chitietsp.html?id=${pId}'">MUA NGAY</button>
                <button class="add-to-cart-btn" onclick="quickAddToCart(${pId})"><i class="fa-solid fa-cart-plus"></i></button>
            </div>
        </div>
        <button class="slider-btn prev" onclick="moveMoiShowcase(-1)">&#10094;</button>
        <button class="slider-btn next" onclick="moveMoiShowcase(1)">&#10095;</button>
    `;
}

function renderVouchers(voucherData) {
    const container = document.getElementById('voucher-list');
    if (!container) return;

    if (!voucherData || voucherData.length === 0) {
        container.innerHTML = "<p style='padding: 20px; color:#888;'>Hiện chưa có mã giảm giá nào.</p>";
        return;
    }

    container.innerHTML = voucherData.map(item => {
        const vCode = item.code || item.Code || "VOUCHER";
        const vType = item.discountType || item.DiscountType;
        const vValue = item.discountValue !== undefined ? item.discountValue : item.DiscountValue;
        const vMinAmount = item.minOrderAmount !== undefined ? item.minOrderAmount : item.MinOrderAmount;
        const vMaxAmount = item.maxDiscountAmount !== undefined ? item.maxDiscountAmount : item.MaxDiscountAmount;

        let discountDesc = "";
        if (vType === "Percent") {
            discountDesc = `Giảm ${parseInt(vValue)}%`;
            if (vMaxAmount) {
                discountDesc += ` (Tối đa ${parseInt(vMaxAmount).toLocaleString('vi-VN')}đ)`;
            }
        } else {
            discountDesc = `Giảm ${parseInt(vValue).toLocaleString('vi-VN')}đ`;
        }

        const conditionText = `Đơn tối thiểu ${parseInt(vMinAmount).toLocaleString('vi-VN')}đ`;

        return `
            <div class="voucher-card" style="min-width: 280px; display: flex; align-items: center;">
                <div class="voucher-left" style="padding: 10px; font-weight: bold; text-align: center;">
                    🎟️ <br> CODE
                </div>
                <div class="voucher-right" style="padding: 10px; flex: 1;">
                    <p class="voucher-code-title" style="font-weight: bold; margin: 0; color: #333;">Mã: ${vCode}</p> 
                    <p class="voucher-desc" style="margin: 3px 0; color: red; font-weight: bold; font-size: 15px;">${discountDesc}</p> 
                    <p class="voucher-condition" style="margin: 0; font-size: 12px; color: #666;">${conditionText}</p>
                    <button class="btn-save" onclick="copyVoucherCode('${vCode}')" style="margin-top: 5px; cursor: pointer;">Sao chép</button>
                </div>
            </div>
        `;
    }).join('');
}

function copyVoucherCode(code) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(code).then(() => {
            alert(`🎉 Đã sao chép mã giảm giá: [ ${code} ] thành công!`);
        }).catch(err => {
            console.error('Không thể sao chép mã: ', err);
        });
    } else {
        const textArea = document.createElement("textarea");
        textArea.value = code;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand("copy");
        document.body.removeChild(textArea);
        alert(`🎉 Đã sao chép mã giảm giá: [ ${code} ]`);
    }
}

async function moveMoiShowcase(direction) {
    if (!newProductsList || newProductsList.length <= 1) return;

    currentMoiIndex += direction;
    if (currentMoiIndex >= newProductsList.length) currentMoiIndex = 0;
    if (currentMoiIndex < 0) currentMoiIndex = newProductsList.length - 1;

    // Lấy ID của sản phẩm tiếp theo trong danh sách slide
    const nextProduct = newProductsList[currentMoiIndex];
    const pId = nextProduct.productId || nextProduct.id || nextProduct.Id;

    // Gọi API chi tiết lấy description xịn từ Database trước khi render
    const rawDetail = await fetchData(`product/${pId}`);
    const fullProduct = rawDetail?.data || rawDetail?.Data || rawDetail || nextProduct;

    renderProductShowcase('showcase-container', fullProduct);
}

// ── 5. LOGIC TRANG DANH MỤC SẢN PHẨM ─────────────────────────────────────
// ✅ SỬA THÀNH HÀM ASYNC để xử lý đợi dữ liệu nếu người dùng vào thẳng bằng link
async function initProductPages() {
    renderVouchers(mockVouchers);

    const gridContainer = document.getElementById('main-product-grid');
    if (!gridContainer) return;

    const params = new URLSearchParams(window.location.search);
    const type = params.get('type');
    const cat = params.get('cat');

    const isNewPage = (type === 'moi');
    const showcaseSection = document.querySelector('.SANPHAMMOI');
    const titleElement = document.getElementById('category-title');
    const hrTag = titleElement ? titleElement.nextElementSibling : null;

    if (isNewPage) {
        if (showcaseSection) {
            showcaseSection.style.display = 'block';

            // 💡 CẢI TIẾN: Nếu mảng sản phẩm mới đang rỗng (do vào thẳng trang danh mục không qua trang chủ)
            // thì lập tức gọi API nạp bù dữ liệu hàng mới ngay lập tức!
            if (!newProductsList || newProductsList.length === 0) {
                const newArrivalsData = await fetchData("product/new-arrivals?limit=5");
                if (newArrivalsData) {
                    newProductsList = newArrivalsData.items || newArrivalsData.Items || (Array.isArray(newArrivalsData) ? newArrivalsData : []);
                }
            }

            if (newProductsList && newProductsList.length > 0) {
                renderProductShowcase('showcase-container', newProductsList[currentMoiIndex]);
            } else {
                const showcase = document.getElementById('showcase-container');
                if (showcase) showcase.innerHTML = "<p style='padding:20px;'>Chưa có sản phẩm mới cập nhật.</p>";
            }
        }
        if (gridContainer) gridContainer.style.display = 'none';
        if (titleElement) titleElement.style.display = 'none';
        if (hrTag && hrTag.tagName === 'HR') hrTag.style.display = 'none';
    } else {
        if (showcaseSection) showcaseSection.style.display = 'none';
        if (gridContainer) gridContainer.style.display = 'grid';
        if (titleElement) titleElement.style.display = 'block';
        if (hrTag && hrTag.tagName === 'HR') hrTag.style.display = 'block';

        let filtered = [...products];

        if (type === 'banchay') {
            filtered.sort((a, b) => {
                const soldA = Number(a.soldCount !== undefined ? a.soldCount : (a.SoldCount || 0));
                const soldB = Number(b.soldCount !== undefined ? b.soldCount : (b.SoldCount || 0));
                return soldB - soldA;
            });
            if (titleElement) titleElement.innerText = "Sản phẩm bán chạy";
        } else if (type === 'khuyenmai') {
            filtered = filtered.filter(p => {
                const disc = validatePrice(p, 'discount', 'Discount');
                return disc > 0;
            });
            if (titleElement) titleElement.innerText = "Sản phẩm khuyến mãi";
        } else if (cat) {
            filtered = filtered.filter(p => {
                const cName = p.category || p.Category;
                return cName === cat;
            });
            if (titleElement) titleElement.innerText = `Danh mục: ${cat}`;
        } else {
            if (titleElement) titleElement.innerText = "Tất cả sản phẩm";
        }

        renderProducts('main-product-grid', filtered);
    }
}

//////////////////////////////////
// ── 6. TRANG CHI TIẾT SẢN PHẨM CHUẨN ĐẦU RA API ───────────────────────────
//////////////////////////
// Định nghĩa các biến toàn cục quản lý trạng thái chọn phân loại trên trang chi tiết
let detailVariantsList = [];
let detailSelectedColor = null;
let detailSelectedSize = null;
let totalProductStock = 0;

async function initDetailPage() {
    const params = new URLSearchParams(window.location.search);
    const productId = parseInt(params.get('id'));
    if (!productId) return;

    const rawResponse = await fetchData(`product/${productId}`);
    if (!rawResponse) {
        console.error("Không nhận được dữ liệu từ API chi tiết sản phẩm.");
        return;
    }

    // Đón dữ liệu bao quát cả chữ hoa lẫn chữ thường
    const product = rawResponse.data || rawResponse.Data || rawResponse;

    if (product) {
        const pName = product.name || product.Name;

        // Bóc tách giá và phần trăm giảm giá từ API gửi về
        const pPrice = Number(product.price || product.Price || 0);
        const pDiscount = Number(product.discount || product.Discount || 0);

        // Xử lý lấy ảnh chính xác từ API
        const pMainImg = product.mainImage || product.MainImage || "";
        const pDesc = product.description || product.Description || 'Chưa có thông tin mô tả cụ thể.';
        const pPolicy = product.policy || product.Policy;

        let fullMainImgUrl = "https://placehold.co/300x400?text=No+Image";
        if (pMainImg) {
            fullMainImgUrl = pMainImg.startsWith("http") ? pMainImg : `${BACKEND_BASE_URL}/${pMainImg}`;
        }

        const hasDiscount = pDiscount > 0;
        const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

        const mainImg = document.getElementById('main-img');
        if (mainImg) mainImg.src = fullMainImgUrl;

        // Xử lý mảng thumbnail ảnh phụ
        const thumbContainer = document.querySelector('.thumbnail-list');
        if (thumbContainer) {
            const finalThumbnails = product.thumbnails || product.Thumbnails || [];
            if (finalThumbnails.length > 0) {
                thumbContainer.innerHTML = finalThumbnails.map(t => {
                    if (!t) return "";
                    const imgPath = typeof t === 'object' ? (t.imageUrl || t.ImageUrl || "") : t;
                    if (!imgPath) return "";
                    const fullThumbUrl = imgPath.startsWith("http") ? imgPath : `${BACKEND_BASE_URL}/${imgPath}`;
                    return `
                        <img src="${fullThumbUrl}" 
                             onclick="document.getElementById('main-img').src=this.src" 
                             alt="thumb" 
                             onerror="this.onerror=null; this.src='https://placehold.co/300x400?text=Image+Error';"
                             style="cursor: pointer; width: 70px; height: 90px; object-fit: cover; border: 1px solid #eee; margin-right: 8px; border-radius: 4px;">
                    `;
                }).join('');
            } else {
                thumbContainer.innerHTML = `
                    <img src="${fullMainImgUrl}" 
                         onclick="document.getElementById('main-img').src=this.src" 
                         alt="thumb"
                         style="cursor: pointer; width: 70px; height: 90px; object-fit: cover; border: 1px solid #eee; border-radius: 4px;">
                `;
            }
        }

        const nameEl = document.getElementById('p-name');
        const priceEl = document.getElementById('p-price');
        if (nameEl) nameEl.innerText = pName;

        if (priceEl) {
            if (hasDiscount) {
                priceEl.innerHTML = `<span style="text-decoration:line-through; color:#aaa; font-size:16px; margin-right:10px;">${formatVND(pPrice)}</span> <span style="color:red; font-weight:bold;">${formatVND(finalPrice)}</span>`;
            } else {
                priceEl.innerText = formatVND(pPrice);
            }
        }

        const discountBadge = document.getElementById('p-discount-badge') || document.querySelector('.badge-discount');
        if (discountBadge) {
            if (hasDiscount) {
                discountBadge.innerText = `-${pDiscount}%`;
                discountBadge.style.display = 'inline-block';
            } else {
                discountBadge.style.display = 'none';
            }
        }

        // Kích hoạt render đánh giá sản phẩm
        if (typeof renderRatingSection === "function") {
            renderRatingSection(product);
        }

        // 💡 LOGIC XỬ LÝ BIẾN THỂ & SỐ LƯỢNG TỒN KHO THỜI GIAN THỰC
        detailVariantsList = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];

        // Reset trạng thái chọn khi tải lại trang chi tiết
        detailSelectedColor = null;
        detailSelectedSize = null;

        // Tính tổng số lượng tồn kho của tất cả các phân loại cộng lại
        totalProductStock = detailVariantsList.reduce((sum, v) => sum + Number(v.stockQuantity || v.StockQuantity || v.stock_quantity || 0), 0);

        // Ban đầu khi người dùng chưa chọn phân loại: Hiển thị tổng số lượng của sản phẩm
        updateDetailStockDisplay(totalProductStock, "tổng");
        enableDetailActionButtons();

        // Trích xuất danh sách Màu sắc và Kích thước duy nhất từ Database trả về
        const colorSet = new Set();
        const sizeSet = new Set();
        detailVariantsList.forEach(v => {
            const c = v.color || v.Color;
            const s = v.size || v.Size;
            if (c) colorSet.add(c);
            if (s) sizeSet.add(s);
        });

        // Khởi tạo render các nút bấm tùy chọn phân loại bằng hàm renderOptions cải tiến bên dưới
        renderDetailOptions('#color-options', Array.from(colorSet).length > 0 ? Array.from(colorSet) : ["Mặc định"], 'color');
        renderDetailOptions('#size-options', Array.from(sizeSet).length > 0 ? Array.from(sizeSet) : ["Freesize"], 'size');

        const descBox = document.getElementById('p-desc');
        if (descBox) {
            descBox.innerHTML = `<p>${pDesc}</p>`;
        }

        // 🌟 XỬ LÝ HIỂN THỊ CHÍNH SÁCH CỬA HÀNG (POLICY) CHUẨN XÁC
        const policyBox = document.getElementById('p-policy');
        if (policyBox) {
            if (pPolicy && pPolicy.trim() !== "") {
                policyBox.innerHTML = `<p>${pPolicy}</p>`;
            } else {
                // Nội dung fallback nếu sản phẩm đó dưới Database chưa được nhập chính sách
                policyBox.innerHTML = `<p>Chưa có thông tin chính sách bảo quản và bán hàng cụ thể cho sản phẩm này.</p>`;
            }
        }

        // Cập nhật lại sự kiện click cho nút mua/thêm giỏ hàng ở trang chi tiết để bóc tách dữ liệu chuẩn
        const detailAddBtn = document.getElementById('detail-add-cart-btn') || document.querySelector('.add-to-cart-btn');
        const detailBuyBtn = document.getElementById('detail-buy-now-btn') || document.querySelector('.buy-now-btn');

        if (detailAddBtn) detailAddBtn.onclick = () => handleDetailCartSubmit(product, 'add_to_cart');
        if (detailBuyBtn) detailBuyBtn.onclick = () => handleDetailCartSubmit(product, 'buy_now');

    } else {
        console.error("Không tìm thấy thông tin chi tiết của sản phẩm này.");
    }
}

// Hàm render nút bấm tùy chọn phân loại chuyên biệt cho trang chi tiết
function renderDetailOptions(containerSelector, optionsArray, type) {
    const container = document.querySelector(containerSelector);
    if (!container) return;

    container.innerHTML = optionsArray.map(opt => {
        return `<button type="button" class="opt-btn detail-v-btn" data-type="${type}" data-value="${opt}" onclick="handleDetailOptionClick(this, '${type}', '${opt}')">${opt}</button>`;
    }).join('');
}

// Xử lý sự kiện click khi chọn Phân loại (Màu hoặc Size)
function handleDetailOptionClick(element, type, value) {
    // Tìm các nút cùng nhóm để gỡ bỏ class active/selected cũ
    const parentContainer = element.parentElement;
    parentContainer.querySelectorAll('.opt-btn').forEach(btn => btn.classList.remove('active', 'selected'));

    // Thêm trạng thái được chọn cho nút hiện tại
    element.classList.add('active', 'selected');

    if (type === 'color') {
        detailSelectedColor = value;
        const colorError = document.getElementById('color-error');
        if (colorError) colorError.style.display = 'none';
    } else if (type === 'size') {
        detailSelectedSize = value;
        const sizeError = document.getElementById('size-error');
        if (sizeError) sizeError.style.display = 'none';
    }

    // Tiến hành kiểm tra kho thời gian thực sau khi người dùng tương tác
    checkDetailSelectionStock();
}

// Hàm kiểm tra tồn kho thời gian thực dựa trên các thuộc tính được chọn
function checkDetailSelectionStock() {
    // 1. Nếu chưa chọn đủ cặp Màu sắc và Kích cỡ -> tiếp tục hiển thị tổng kho sản phẩm
    if (!detailSelectedColor || !detailSelectedSize) {
        updateDetailStockDisplay(totalProductStock, "tổng");
        enableDetailActionButtons();
        return;
    }

    // 2. Tìm phân loại khớp hoàn toàn với Màu và Size đã chọn trong mảng dữ liệu
    const matchedVariant = detailVariantsList.find(v =>
        (v.color || v.Color || '').toLowerCase() === detailSelectedColor.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === detailSelectedSize.toLowerCase()
    );

    if (matchedVariant) {
        const stock = Number(matchedVariant.stockQuantity || matchedVariant.StockQuantity || matchedVariant.stock_quantity || 0);
        updateDetailStockDisplay(stock, "phân loại");

        // Chặn tuyệt đối nếu số lượng phân loại này bằng 0 (Hết hàng)
        if (stock === 0) {
            disableDetailActionButtons("Hết hàng");
        } else {
            enableDetailActionButtons();
        }
    } else {
        // Cặp Màu + Size này không tồn tại hoặc chưa cấu hình dưới Database
        updateDetailStockDisplay(0, "phân loại");
        disableDetailActionButtons("Không có sẵn");
    }
}

// Cập nhật text trạng thái số lượng kho lên UI trang chi tiết
function updateDetailStockDisplay(quantity, displayType) {
    // Tìm thẻ hiển thị trạng thái tồn kho trên trang chi tiết (Bổ sung id này vào HTML nếu chưa có)
    const stockStatusEl = document.getElementById('detail-stock-status') || document.getElementById('p-stock-status');
    if (!stockStatusEl) return;

    if (quantity === 0) {
        stockStatusEl.innerText = "Hết hàng";
        stockStatusEl.style.color = "red";
    } else {
        stockStatusEl.innerText = displayType === "tổng"
            ? `${quantity} sản phẩm có sẵn`
            : `Còn lại ${quantity} sản phẩm`;
        stockStatusEl.style.color = "#2ecc71"; // Màu xanh lá cây
    }
}

// Khóa tương tác người dùng khi phân loại hết hàng
function disableDetailActionButtons(reason) {
    const addCartBtn = document.getElementById('detail-add-cart-btn') || document.querySelector('.add-to-cart-btn');
    const buyNowBtn = document.getElementById('detail-buy-now-btn') || document.querySelector('.buy-now-btn');

    if (addCartBtn) {
        addCartBtn.disabled = true;
        addCartBtn.classList.add('disabled-btn');
        addCartBtn.innerHTML = reason === "Hết hàng" ? `<i class="fa-solid fa-ban"></i> Hết hàng` : "Không có sẵn";
    }
    if (buyNowBtn) {
        buyNowBtn.disabled = true;
        buyNowBtn.classList.add('disabled-btn');
        buyNowBtn.innerText = "Không thể mua";
    }
}

// Mở khóa tương tác người dùng khi có hàng trở lại
function enableDetailActionButtons() {
    const addCartBtn = document.getElementById('detail-add-cart-btn') || document.querySelector('.add-to-cart-btn');
    const buyNowBtn = document.getElementById('detail-buy-now-btn') || document.querySelector('.buy-now-btn');

    if (addCartBtn) {
        addCartBtn.disabled = false;
        addCartBtn.classList.remove('disabled-btn');
        addCartBtn.innerHTML = `<i class="fa-solid fa-cart-plus"></i> THÊM VÀO GIỎ HÀNG`;
    }
    if (buyNowBtn) {
        buyNowBtn.disabled = false;
        buyNowBtn.classList.remove('disabled-btn');
        buyNowBtn.innerText = "MUA NGAY";
    }
}

// Xử lý gửi dữ liệu đặt mua an toàn từ trang chi tiết
async function handleDetailCartSubmit(product, actionType) {
    const qtyInput = document.getElementById('qty') || document.getElementById('detail-qty-input');
    const quantity = qtyInput ? parseInt(qtyInput.value) : 1;

    let valid = true;
    const colorError = document.getElementById('color-error');
    if (!detailSelectedColor) {
        if (colorError) colorError.style.display = 'block';
        valid = false;
    }
    const sizeError = document.getElementById('size-error');
    if (!detailSelectedSize) {
        if (sizeError) sizeError.style.display = 'block';
        valid = false;
    }


    // Khớp thực thể biến thể tương ứng dưới Local Memory
    const matchedVariant = detailVariantsList.find(v =>
        (v.color || v.Color || '').toLowerCase() === detailSelectedColor.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === detailSelectedSize.toLowerCase()
    );

    const stock = matchedVariant ? Number(matchedVariant.stockQuantity || matchedVariant.StockQuantity || matchedVariant.stock_quantity || 0) : 0;

    // Bảo vệ chặn tầng logic nếu số lượng bằng 0 hoặc vượt mức kho
    if (stock === 0) {
        alert("Phân loại sản phẩm này đã hết hàng, vui lòng chọn phân loại khác!");
        return;
    }
    if (quantity > stock) {
        alert(`Số lượng bạn chọn vượt quá số lượng hàng có sẵn trong kho (Hiện còn: ${stock})`);
        return;
    }

    const pId = product.productId || product.ProductId || product.id || product.Id;
    const pName = product.name || product.Name;
    const pPrice = validatePrice(product, 'price', 'Price');
    const pDiscount = validatePrice(product, 'discount', 'Discount');
    const pMainImg = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || product.image || "";

    let fullMainImgUrl = "https://placehold.co/300x400?text=No+Image";
    if (pMainImg) {
        fullMainImgUrl = pMainImg.startsWith("http") ? pMainImg : `${BACKEND_BASE_URL}/${pMainImg}`;
    }

    const vId = matchedVariant ? (matchedVariant.variantId || matchedVariant.VariantId || matchedVariant.variant_id) : 0;

    const cartItem = {
        productId: pId,
        variantId: vId,
        name: pName,
        price: pPrice,
        discount: pDiscount,
        mainImage: fullMainImgUrl,
        color: detailSelectedColor,
        size: detailSelectedSize,
        quantity: quantity
    };

    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    const existingIndex = cart.findIndex(item =>
        item.productId === cartItem.productId && item.variantId === cartItem.variantId
    );

    if (existingIndex > -1) {
        if (cart[existingIndex].quantity + quantity > stock) {
            alert(`Giỏ hàng của bạn đã có ${cart[existingIndex].quantity} sản phẩm này. Không thể thêm tiếp vì vượt quá số lượng tồn kho là ${stock}!`);
            return;
        }
        cart[existingIndex].quantity += quantity;
    } else {
        cart.push(cartItem);
    }

    localStorage.setItem('cart', JSON.stringify(cart));
    if (typeof showSuccessToast === "function") showSuccessToast();
    updateCartBadge();

    if (actionType === 'buy_now') {
        setTimeout(() => { window.location.href = "giohang.html"; }, 300);
    } 
}
function renderRatingSection(product) {
    if (!product) return;

    // 1. Render số sao tổng quan ở phía trên (Cạnh tên sản phẩm)
    const ratingSummaryEl = document.getElementById('p-rating-summary');
    if (ratingSummaryEl) {
        const avgRating = product.averageRating || product.AverageRating || 0;
        const reviewCount = product.reviewCount || product.ReviewCount || 0;
        const soldCount = product.soldCount || product.SoldCount || 0;

        const stars = '★'.repeat(Math.round(avgRating)) + '☆'.repeat(5 - Math.round(avgRating));
        ratingSummaryEl.innerHTML = `
            <span style="color: #ffc107; font-size: 16px;">${stars}</span> 
            <span style="margin-left: 5px; color: #666;">(${avgRating}/5)</span>
            <span style="margin-left: 15px; color: #888;">| Đã bán: ${soldCount}</span>
        `;
    }

    // 2. Render danh sách các bình luận chi tiết ở phía dưới
    const reviews = product.reviews || product.Reviews || product.recentReviews || product.RecentReviews || [];
    const reviewContainer = document.getElementById('review-container');

    if (!reviewContainer) return;

    if (reviews.length === 0) {
        reviewContainer.innerHTML = `<p class="text-muted" style="padding: 15px 0;">Sản phẩm này chưa có đánh giá nào từ người mua.</p>`;
        return;
    }

    // Lặp mảng dữ liệu đổ dữ liệu thật ra giao diện
    reviewContainer.innerHTML = reviews.map(r => {
        const name = r.customerName || r.CustomerName || "Khách hàng ẩn danh";
        const rating = r.rating || r.Rating || 5;
        const comment = r.comment || r.Comment || "Người mua không để lại bình luận.";
        const date = r.createdAt || r.CreatedAt
            ? new Date(r.createdAt || r.CreatedAt).toLocaleDateString('vi-VN')
            : "Vừa xong";

        return `
            <div class="review-item" style="border-bottom: 1px solid #eee; padding: 15px 0;">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <strong style="color: #333;">${name}</strong>
                    <small class="text-muted">${date}</small>
                </div>
                <div style="color: #ffc107; margin: 5px 0;">${'★'.repeat(rating)}${'☆'.repeat(5 - rating)}</div>
                <p style="margin: 5px 0 0 0; color: #555; font-size: 14px;">${comment}</p>
            </div>
        `;
    }).join('');
}



// =========================================================================
// ── 7. KHỐI LOGIC GIỎ HÀNG (ĐÃ TỐI ƯU SỬ DỤNG TEMPLATE & ĐỒNG BỘ LOGIC) ──
// =========================================================================

async function addToCart() {
    const params = new URLSearchParams(window.location.search);
    const productId = parseInt(params.get('id'));

    const rawRes = await fetchData(`product/${productId}`);
    if (!rawRes) return;
    const product = rawRes.data || rawRes.Data || rawRes;

    const selectedColor = document.querySelector('#color-options .opt-btn.active');
    const selectedSize = document.querySelector('#size-options .opt-btn.active');
    const qtyInput = document.getElementById('qty');
    const quantity = qtyInput ? parseInt(qtyInput.value) : 1;

    let valid = true;
    const colorError = document.getElementById('color-error');
    if (!selectedColor) {
        if (colorError) colorError.style.display = 'block';
        valid = false;
    }
    const sizeError = document.getElementById('size-error');
    if (!selectedSize) {
        if (sizeError) sizeError.style.display = 'block';
        valid = false;
    }
    if (!valid) return;

    const pId = product.productId || product.ProductId || product.id || product.Id;
    const pName = product.name || product.Name;

    const pPrice = validatePrice(product, 'price', 'Price');
    const pDiscount = validatePrice(product, 'discount', 'Discount');
    const pMainImg = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || product.image || "";

    let fullMainImgUrl = "https://placehold.co/300x400?text=No+Image";
    if (pMainImg) {
        fullMainImgUrl = pMainImg.startsWith("http") ? pMainImg : `${BACKEND_BASE_URL}/${pMainImg}`;
    }

    const colorText = selectedColor.innerText;
    const sizeText = selectedSize.innerText;

    const variants = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];
    const matchedVariant = variants.find(v =>
        (v.color || v.Color || '').toLowerCase() === colorText.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === sizeText.toLowerCase()
    );

    const vId = matchedVariant ? (matchedVariant.variantId || matchedVariant.VariantId || matchedVariant.id || 0) : (variants[0]?.variantId || 0);

    const cartItem = {
        productId: pId,
        variantId: vId,
        name: pName,
        price: pPrice,
        discount: pDiscount,
        mainImage: fullMainImgUrl,
        color: colorText,
        size: sizeText,
        quantity: quantity
    };

    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    const existingIndex = cart.findIndex(item =>
        item.productId === cartItem.productId && item.variantId === cartItem.variantId
    );

    if (existingIndex > -1) {
        cart[existingIndex].quantity += quantity;
    } else {
        cart.push(cartItem);
    }

    localStorage.setItem('cart', JSON.stringify(cart));
    showSuccessToast();
    updateCartBadge();

    setTimeout(() => { window.location.href = "giohang.html"; }, 800);
}

async function quickAddToCart(productId) {
    const rawRes = await fetchData(`product/${productId}`);
    if (!rawRes) return;
    const product = rawRes.data || rawRes.Data || rawRes;

    const pId = product.productId || product.ProductId || product.id || product.Id;
    const pName = product.name || product.Name;

    const pPrice = validatePrice(product, 'price', 'Price');
    const pDiscount = validatePrice(product, 'discount', 'Discount');
    const pMainImg = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || product.image || "";

    let fullMainImgUrl = "https://placehold.co/300x400?text=No+Image";
    if (pMainImg) {
        fullMainImgUrl = pMainImg.startsWith("http") ? pMainImg : `${BACKEND_BASE_URL}/${pMainImg}`;
    }

    const variants = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];
    const defaultVariant = variants[0];
    const vId = defaultVariant ? (defaultVariant.variantId || defaultVariant.VariantId || defaultVariant.id || 0) : 0;
    const vColor = defaultVariant ? (defaultVariant.color || defaultVariant.Color) : "Mặc định";
    const vSize = defaultVariant ? (defaultVariant.size || defaultVariant.Size) : "Freesize";

    const cartItem = {
        productId: pId,
        variantId: vId,
        name: pName,
        price: pPrice,
        discount: pDiscount,
        mainImage: fullMainImgUrl,
        color: vColor,
        size: vSize,
        quantity: 1
    };

    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    const existingIndex = cart.findIndex(item => item.productId === cartItem.productId && item.variantId === cartItem.variantId);

    if (existingIndex > -1) {
        cart[existingIndex].quantity += 1;
    } else {
        cart.push(cartItem);
    }

    localStorage.setItem('cart', JSON.stringify(cart));
    showSuccessToast();
    updateCartBadge();
}

function renderCart() {
    const container = document.getElementById('cart-items-list');
    const templateEl = document.getElementById('cart-item-template');
    if (!container || !templateEl) return;

    const cart = JSON.parse(localStorage.getItem('cart')) || [];

    if (cart.length === 0) {
        container.innerHTML = `<div style="text-align:center; padding:50px;">Giỏ hàng của bạn đang trống</div>`;
        const totalAmountEl = document.getElementById('cart-total-amount');
        if (totalAmountEl) totalAmountEl.innerText = "0đ";
        return;
    }

    // Đọc mã khung sườn HTML mẫu từ thẻ template thiết kế ở trang HTML
    const templateHtml = templateEl.innerHTML;

    container.innerHTML = cart.map((item, index) => {
        const itemDiscount = Number(item.discount || 0);
        const finalPrice = itemDiscount > 0 ? item.price * (1 - itemDiscount / 100) : item.price;
        const subtotalValue = finalPrice * item.quantity;

        const itemImg = item.mainImage ? (item.mainImage.startsWith("http") ? item.mainImage : `${BACKEND_BASE_URL}/${item.mainImage}`) : 'https://placehold.co/300x400?text=No+Image';

        const discountRow = itemDiscount > 0
            ? `<span style="text-decoration:line-through; color:#aaa; font-size:12px; display:block;">${formatVND(item.price * item.quantity)}</span>`
            : '';

        // Điền dữ liệu thực tế vào các vị trí trống được đánh dấu trong thẻ template HTML
        return templateHtml
            .replaceAll('{index}', index)
            .replaceAll('{productId}', item.productId)
            .replaceAll('{variantId}', item.variantId)
            .replace('{itemImg}', itemImg)
            .replaceAll('{name}', item.name)
            .replace('{color}', item.color || 'Mặc định')
            .replace('{size}', item.size || 'Freesize')
            .replace('{quantity}', item.quantity)
            .replace('{discountRow}', discountRow)
            .replace('{subtotal}', formatVND(subtotalValue));
    }).join('');

    const totalAmountEl = document.getElementById('cart-total-amount');
    if (totalAmountEl) totalAmountEl.innerText = "0đ";

    // Khởi động các sự kiện và đồng bộ hóa nút chọn
    initCheckAllEvents();

    // Đưa trạng thái các ô chọn tất cả về trạng thái chưa tích mặc định khi tải lại
    const checkAllTop = document.getElementById('check-all-top');
    const checkAllBottom = document.getElementById('check-all-bottom');
    if (checkAllTop) checkAllTop.checked = false;
    if (checkAllBottom) checkAllBottom.checked = false;

    handleAutoCheckBuyNow();
}

function updateCartQty(index, delta) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    if (!cart[index] || cart[index].quantity + delta < 1) return;

    cart[index].quantity += delta;
    localStorage.setItem('cart', JSON.stringify(cart));

    const row = document.querySelector(`.cart-item-row[data-index="${index}"]`);
    if (row) {
        const qtySpan = row.querySelector('.qty-control span');
        if (qtySpan) qtySpan.innerText = cart[index].quantity;

        const item = cart[index];
        const itemDiscount = Number(item.discount || 0);
        const finalPrice = itemDiscount > 0 ? item.price * (1 - itemDiscount / 100) : item.price;
        const subtotal = finalPrice * item.quantity;

        const priceContainer = row.querySelector('.cart-item-price');
        if (priceContainer) {
            if (itemDiscount > 0) {
                priceContainer.innerHTML = `
                    <span style="text-decoration:line-through; color:#aaa; font-size:12px; display:block;">${formatVND(item.price * item.quantity)}</span>
                    <span class="actual-price" style="font-weight:bold; color:red;">${formatVND(subtotal)}</span>
                `;
            } else {
                priceContainer.innerHTML = `<span class="actual-price" style="font-weight:bold; color:red;">${formatVND(subtotal)}</span>`;
            }
        }
    }
    updateCartBadge();
    updateTotalPrice();
}

function removeFromCart(index) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    cart.splice(index, 1);
    localStorage.setItem('cart', JSON.stringify(cart));
    renderCart();
    updateCartBadge();
    updateTotalPrice();
}

function updateCartBadge() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const totalItems = cart.reduce((sum, item) => sum + item.quantity, 0);
    const badge = document.getElementById('cart-count');
    if (!badge) return;
    badge.innerText = totalItems <= 0 ? "0" : (totalItems > 9 ? "9+" : totalItems);
}

function updateTotalPrice() {
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const rows = document.querySelectorAll('.cart-item-row');
    const totalAmountEl = document.getElementById('cart-total-amount');

    let totalSelected = 0;
    rows.forEach(row => {
        const checkbox = row.querySelector('.item-checkbox');
        if (checkbox && checkbox.checked) {
            const index = row.getAttribute('data-index');
            const item = cart[index];
            if (item) {
                const itemDiscount = Number(item.discount || 0);
                const finalPrice = itemDiscount > 0 ? item.price * (1 - itemDiscount / 100) : item.price;
                totalSelected += finalPrice * item.quantity;
            }
        }
    });
    if (totalAmountEl) totalAmountEl.innerText = formatVND(totalSelected);

    checkAndSyncMasterCheckbox();
}

function handleAutoCheckBuyNow() {
    const autoCheckData = localStorage.getItem('auto_check_item');
    if (!autoCheckData) return;

    const target = JSON.parse(autoCheckData);

    // SỬA TẠI ĐÂY: Tăng nhẹ thời gian chờ lên 200ms để trình duyệt kịp cập nhật cây DOM từ template mẫu
    setTimeout(() => {
        const checkboxes = document.querySelectorAll('.cart-item-checkbox');
        let isItemFoundAndChecked = false;

        checkboxes.forEach(checkbox => {
            const pId = checkbox.getAttribute('data-product-id');
            const vId = checkbox.getAttribute('data-variant-id');

            // Ép kiểu về String để so sánh chính xác tuyệt đối, tránh lỗi lệch kiểu dữ liệu (String vs Number)
            if (String(pId) === String(target.productId) && String(vId) === String(target.variantId)) {
                checkbox.checked = true;
                isItemFoundAndChecked = true;
            }
        });

        if (isItemFoundAndChecked) {
            // Xóa dấu vết sau khi đã tích chọn thành công
            localStorage.removeItem('auto_check_item');

            // Tính toán và nhảy số tiền tổng ngay lập tức
            if (typeof updateTotalPrice === 'function') {
                updateTotalPrice();
            }
        }
    }, 200);
}

// Hàm xử lý đồng bộ sự kiện click cho các ô "Chọn tất cả" tránh bị lặp listener khi render lại DOM
function initCheckAllEvents() {
    const checkAllTop = document.getElementById('check-all-top');
    const checkAllBottom = document.getElementById('check-all-bottom');

    if (!checkAllTop || !checkAllBottom) return;

    // Gán trực tiếp qua hàm ẩn danh để tự động loại bỏ (overwrite) sự kiện cũ nếu có
    checkAllTop.onchange = function () {
        const isChecked = checkAllTop.checked;
        checkAllBottom.checked = isChecked;
        toggleAllItemCheckboxes(isChecked);
    };

    checkAllBottom.onchange = function () {
        const isChecked = checkAllBottom.checked;
        checkAllTop.checked = isChecked;
        toggleAllItemCheckboxes(isChecked);
    };
}

function toggleAllItemCheckboxes(status) {
    const itemCheckboxes = document.querySelectorAll('.cart-item-checkbox');
    itemCheckboxes.forEach(cb => {
        cb.checked = status;
    });

    if (typeof updateTotalPrice === 'function') {
        updateTotalPrice();
    }
}

function checkAndSyncMasterCheckbox() {
    const checkAllTop = document.getElementById('check-all-top');
    const checkAllBottom = document.getElementById('check-all-bottom');
    const itemCheckboxes = document.querySelectorAll('.cart-item-checkbox');

    if (itemCheckboxes.length === 0) {
        if (checkAllTop) checkAllTop.checked = false;
        if (checkAllBottom) checkAllBottom.checked = false;
        return;
    }

    const allChecked = Array.from(itemCheckboxes).every(cb => cb.checked);

    if (checkAllTop) checkAllTop.checked = allChecked;
    if (checkAllBottom) checkAllBottom.checked = allChecked;
}

function changeQty(val) {
    const input = document.getElementById('qty');
    if (!input) return;
    let v = parseInt(input.value) + val;
    if (v >= 1) input.value = v;
}

function moveSlider(id, direction) {
    const container = document.getElementById(id);
    if (container) {
        const scrollAmount = id === 'voucher-list' ? 320 : 270;
        container.scrollBy({ left: direction * scrollAmount, behavior: 'smooth' });
    }
}

function showModal(title, message) {
    const modal = document.getElementById('custom-alert');
    const titleEl = document.getElementById('modal-title');
    const messageEl = document.getElementById('modal-message');
    if (modal) {
        if (titleEl && title) titleEl.innerText = title;
        if (messageEl && message) messageEl.innerText = message;
        modal.style.display = 'flex';
    }
}

function closeModal() {
    const modal = document.getElementById('custom-alert');
    if (modal) modal.style.display = 'none';
}

function showSuccessToast() {
    const toast = document.getElementById('cart-toast');
    if (toast) {
        toast.style.display = 'block';
        setTimeout(() => toast.style.display = 'none', 2000);
    }
}

// 🌟 SỬA HOÀN CHỈNH ĐOẠN KHUYẾT CÚ PHÁP CUỐI FILE CỦA BẠN
function renderOptions(selector, data) {
    const container = document.querySelector(selector);
    if (!container) return;

    container.innerHTML = data.map(item =>
        `<button class="opt-btn" onclick="this.parentNode.querySelectorAll('.opt-btn').forEach(b=>b.classList.remove('active')); this.classList.add('active');">${item}</button>`
    ).join('');
}

let currentSelectedProductId = null;
let currentActionType = ''; // 'add_to_cart' hoặc 'buy_now'
let selectedVariantObj = null; // 🌟 SỬA: Lưu nguyên Object phân loại được chọn để lấy chữ Màu/Size
let currentProductData = null; // 🌟 BỔ SUNG: Lưu thông tin gốc của sản phẩm để lấy tên, giá, ảnh

// 1. Hàm mở Modal và lấy thông tin chi tiết sản phẩm
async function openQuickVariantModal(productId, actionType) {
    currentSelectedProductId = productId;
    currentActionType = actionType;
    selectedVariantObj = null; // Reset biến chọn
    currentProductData = null; // Reset dữ liệu gốc
    document.getElementById('q-modal-qty').value = 1; // Reset số lượng mua về 1

    try {
        // Gọi API chi tiết sản phẩm
        const res = await fetchData(`product/${productId}`);
        const product = res?.data || res?.Data || res;

        if (!product) return;
        currentProductData = product; // Lưu lại để dùng khi bấm XÁC NHẬN

        // Điền thông tin cơ bản lên Pop-up
        document.getElementById('q-modal-name').innerText = product.name || product.Name;
        document.getElementById('q-modal-price').innerText = formatVND(product.price || product.Price);

        let pImg = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || "";
        document.getElementById('q-modal-img').src = pImg.startsWith('http') ? pImg : `${BACKEND_BASE_URL}/${pImg}`;

        const variants = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];
        const variantContainer = document.getElementById('q-variant-list');

        if (variants.length === 0) {
            variantContainer.innerHTML = `<p class="text-muted" style="font-size: 13px; color: #999;">Sản phẩm hiện đang tạm hết hàng.</p>`;
            return;
        }

        // Tạo giao diện nút bấm ghép nối "Màu sắc - Kích thước"
        variantContainer.innerHTML = variants.map(v => {
            let displayLabel = "";
            if (v.color && v.size) {
                displayLabel = `${v.color} - ${v.size}`;
            } else {
                displayLabel = v.color || v.size || `Phân loại #${v.variantId || v.id}`;
            }

            let stockInfo = v.stockQuantity > 0 ? `(Còn ${v.stockQuantity})` : `(Hết hàng)`;
            let isOutOfStock = v.stockQuantity <= 0;
            const vId = v.variantId || v.VariantId || v.id || 0;

            // Truyền cả đối tượng Variant vào hàm onclick thông qua JSON chuỗi hóa
            return `
                <button type="button" 
                        class="v-option-btn ${isOutOfStock ? 'disabled-stock-btn' : ''}" 
                        data-id="${vId}" 
                        ${isOutOfStock ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}
                        onclick="selectProductVariant(this, ${vId}, '${encodeURIComponent(JSON.stringify(v))}')">
                    ${displayLabel} <span style="font-size: 11px; color: #777;">${stockInfo}</span>
                </button>
            `;
        }).join('');

        document.getElementById('quick-variant-modal').style.display = 'flex';

    } catch (err) {
        console.error("Lỗi khi mở popup phân loại:", err);
    }
}

// 2. Hàm đóng Modal
function closeQuickVariantModal() {
    document.getElementById('quick-variant-modal').style.display = 'none';
}

// 3. Hàm xử lý khi bấm chọn 1 phân loại cụ thể
function selectProductVariant(buttonElement, variantId, encodedVariantStr) {
    const buttons = document.querySelectorAll('.v-option-btn');
    buttons.forEach(btn => btn.classList.remove('selected'));

    buttonElement.classList.add('selected');

    // Giải mã ngược chuỗi thu được để lấy cấu trúc Object thực tế của biến thể đó
    selectedVariantObj = JSON.parse(decodeURIComponent(encodedVariantStr));
}

// 4. Bắt sự kiện khi bấm nút "XÁC NHẬN" trên Pop-up
document.getElementById('q-modal-submit-btn').onclick = function () {
    const qty = parseInt(document.getElementById('q-modal-qty').value) || 1;

    if (!selectedVariantObj || !currentProductData) {
        alert("Vui lòng chọn một phân loại sản phẩm trước khi xác nhận!");
        return;
    }

    const pId = currentProductData.productId || currentProductData.ProductId || currentProductData.id || currentProductData.Id;
    const pName = currentProductData.name || currentProductData.Name;
    const pPrice = validatePrice(currentProductData, 'price', 'Price');
    const pDiscount = validatePrice(currentProductData, 'discount', 'Discount');
    const pMainImg = currentProductData.mainImage || currentProductData.MainImage || currentProductData.imageUrl || currentProductData.ImageUrl || "";

    let fullMainImgUrl = "https://placehold.co/300x400?text=No+Image";
    if (pMainImg) {
        fullMainImgUrl = pMainImg.startsWith("http") ? pMainImg : `${BACKEND_BASE_URL}/${pMainImg}`;
    }

    const vId = selectedVariantObj.variantId || selectedVariantObj.VariantId || selectedVariantObj.id || 0;
    const vColor = selectedVariantObj.color || selectedVariantObj.Color || "Mặc định";
    const vSize = selectedVariantObj.size || selectedVariantObj.Size || "Freesize";

    // 🌟 SỬA ĐỒNG BỘ: Tạo cấu trúc Object cartItem khớp hoàn toàn với hàm addToCart() cũ
    const cartItem = {
        productId: pId,
        variantId: vId,
        name: pName,
        price: pPrice,
        discount: pDiscount,
        mainImage: fullMainImgUrl,
        color: vColor,
        size: vSize,
        quantity: qty
    };

    // 🌟 SỬA ĐỒNG BỘ: Chuyển từ 'shopping_cart' thành 'cart' để chạy chung một bộ nhớ localStorage
    let cart = JSON.parse(localStorage.getItem('cart')) || [];

    const existingIndex = cart.findIndex(item => item.productId === cartItem.productId && item.variantId === cartItem.variantId);
    if (existingIndex > -1) {
        cart[existingIndex].quantity += qty;
    } else {
        cart.push(cartItem);
    }

    localStorage.setItem('cart', JSON.stringify(cart));

    // Cập nhật lại số lượng giỏ hàng trên Header (nếu có hàm badge của bạn)
    if (typeof updateCartBadge === 'function') {
        updateCartBadge();
    } else if (typeof updateCartCountHeader === 'function') {
        updateCartCountHeader();
    }

    // Phân tách hành động
    if (currentActionType === 'add_to_cart') {
        // Nếu trang web có sử dụng thư viện thông báo Toast, bạn có thể thay alert bằng showSuccessToast()
        if (typeof showSuccessToast === 'function') {
            showSuccessToast();
        } 
        closeQuickVariantModal();
    }
    else if (currentActionType === 'buy_now') {
        // Đánh dấu lại id phân loại vừa mua để trang giohang.html tự tích chọn
        localStorage.setItem('auto_check_item', JSON.stringify({
            productId: cartItem.productId,
            variantId: cartItem.variantId
        }));

        // Chuyển hướng thẳng sang giỏ hàng
        window.location.href = 'giohang.html';
    }
};

