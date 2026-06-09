const API_BASE_URL = "https://localhost:5001/api";
const BACKEND_BASE_URL = "https://localhost:5001";

let products = [];
let mockVouchers = [];
let newProductsList = [];
let currentMoiIndex = 0;

// Hàm định dạng tiền tệ VND dùng chung (Đã tối ưu sửa lỗi NaN)
const formatVND = (amount) => {
    const parsedAmount = Number(amount);
    if (amount === undefined || amount === null || isNaN(parsedAmount) || parsedAmount === 0) return '0đ';
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
        localStorage.removeItem('checkout_voucher');
        renderCart();
    }
});

//=======================================
// ── 3. KHỞI TẠO DỮ LIỆU CHO TRANG CHỦ 
//=======================================
async function initHomePage() {
    if (mockVouchers && mockVouchers.length > 0) {
        renderVouchers(mockVouchers);
    }

    console.log("[Logic Log] Bắt đầu nạp dữ liệu phân loại trang chủ...");

    const bestSellersData = await fetchData("product/best-sellers?limit=4");
    const newArrivalsData = await fetchData("product/new-arrivals?limit=5");
    const discountsData = await fetchData("product/discounts?limit=4");

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

//=============================================
// ── 4. HÀM RENDER HTML SẢN PHẨM 
//========================================
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


//===========================
//    SẢN PHẨM MỚI
//===========================
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


//=============================
//         VOUCHER
//=========================
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
            showModal("Thành công",`🎉 Đã sao chép mã giảm giá: [ ${code} ] thành công!`);
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
        showModal("Thành công", `🎉 Đã sao chép mã giảm giá: [ ${code} ] !`);
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

//================================================
// ── 5. LOGIC TRANG DANH MỤC SẢN PHẨM 
// ==========================================================
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

//==========================================
// 6. TRANG CHI TIẾT SẢN PHẨM 
//=================================
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

// Xử lý gửi dữ liệu đặt mua AN TOÀN và ĐỒNG BỘ qua API từ trang chi tiết
async function handleDetailCartSubmit(product, actionType) {
    const qtyInput = document.getElementById('qty') || document.getElementById('detail-qty-input');
    const quantity = qtyInput ? parseInt(qtyInput.value) : 1;

    // 1. Kiểm tra xem người dùng đã chọn đầy đủ phân loại chưa
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
    if (!valid) return; // Dừng lại nếu chưa chọn đủ thuộc tính

    // 2. Kiểm tra trạng thái đăng nhập (Lấy Token từ LocalStorage)
    const token = localStorage.getItem('accessToken');
    if (!token) {
        alert("Vui lòng đăng nhập tài khoản trước khi thực hiện thêm vào giỏ hàng!");
        window.location.href = "login.html";
        return;
    }

    // 3. Khớp thực thể biến thể tương ứng dưới Local Memory để lấy VariantId
    const matchedVariant = detailVariantsList.find(v =>
        (v.color || v.Color || '').toLowerCase() === detailSelectedColor.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === detailSelectedSize.toLowerCase()
    );

    const stock = matchedVariant ? Number(matchedVariant.stockQuantity || matchedVariant.StockQuantity || matchedVariant.stock_quantity || 0) : 0;

    // Bảo vệ chặn tầng logic nếu số lượng bằng 0 hoặc vượt mức kho
    if (stock === 0) {
        if (typeof showModal === "function") {
            showModal("Hết hàng", 'Phân loại sản phẩm này đã hết hàng, vui lòng chọn phân loại khác!');
        } else {
            alert('Phân loại sản phẩm này đã hết hàng, vui lòng chọn phân loại khác!');
        }
        return;
    }
    if (quantity > stock) {
        if (typeof showModal === "function") {
            showModal("Ôi nooo!!!", `Số lượng bạn chọn vượt quá số lượng hàng có sẵn trong kho (Hiện còn: ${stock})`);
        } else {
            alert(`Số lượng bạn chọn vượt quá số lượng hàng có sẵn trong kho (Hiện còn: ${stock})`);
        }
        return;
    }

    const pId = product.productId || product.ProductId || product.id || product.Id;
    const vId = matchedVariant ? (matchedVariant.variantId || matchedVariant.VariantId || matchedVariant.variant_id) : 0;

    // 4. Định hình gói tin dữ liệu khớp CHUẨN 100% với `AddToCartDto` trong C# của bạn
    const apiPayload = {
        ProductId: pId,
        VariantId: vId,
        Quantity: quantity
    };

    try {
        // 5. Tiến hành bắn Fetch API POST lên Backend Server
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${token}` // Đính kèm JWT Token để qua cửa ải [Authorize]
            },
            body: JSON.stringify(apiPayload)
        });

        const result = await response.json();

        if (response.ok && result.success) {
            // Hiện Toast hoặc thông báo thành công
            if (typeof showSuccessToast === "function") {
                showSuccessToast();
            } else {
                alert("Đã thêm sản phẩm vào giỏ hàng thành công!");
            }

            // Cập nhật lại số hiển thị trên icon giỏ hàng ở Header đại diện
            if (typeof updateCartBadge === "function") updateCartBadge();

            // Nếu người dùng nhấn nút "MUA NGAY", chuyển hướng thẳng sang trang giỏ hàng
            if (actionType === 'buy_now') {
                const autoCheckObj = {
                    productId: pId,
                    variantId: vId
                };
                localStorage.setItem('auto_check_item', JSON.stringify(autoCheckObj));

                setTimeout(() => {
                    window.location.href = "giohang.html";
                }, 300);
            }
        } else {
            // Trường hợp Backend trả về lỗi do hết hàng ngầm hoặc token hết hạn
            alert(result.message || "Không thể thêm sản phẩm vào giỏ hàng. Vui lòng thử lại.");
            if (response.status === 401) {
                window.location.href = "login.html";
            }
        }

    } catch (error) {
        console.error("Error Detail Add To Cart:", error);
        alert("Lỗi kết nối máy chủ API. Không thể thêm sản phẩm vào giỏ hàng.");
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


//======================================================================
// LOGIC ĐIỀU KHIỂN POP-UP GIỎ HÀNG / MUA HÀNG QUA API BACKEND
//======================================================================
let currentSelectedProductId = null;
let currentActionType = ''; // 'add_to_cart' hoặc 'buy_now'
let selectedVariantObj = null;
let currentProductData = null;

// Hàm mở Modal và lấy thông tin chi tiết sản phẩm
async function openQuickVariantModal(productId, actionType) {
    currentSelectedProductId = productId;
    currentActionType = actionType;
    selectedVariantObj = null;
    currentProductData = null;

    const qQtyInput = document.getElementById('q-modal-qty');
    if (qQtyInput) qQtyInput.value = 1;

    try {
        const res = await fetchData(`product/${productId}`);
        const product = res?.data || res?.Data || res;

        if (!product) return;
        currentProductData = product;

        const qNameEl = document.getElementById('q-modal-name');
        const qPriceEl = document.getElementById('q-modal-price');
        const qImgEl = document.getElementById('q-modal-img');

        if (qNameEl) qNameEl.innerText = product.name || product.Name;
        if (qPriceEl) qPriceEl.innerText = formatVND(product.price || product.Price);

        let pImg = product.mainImage || product.MainImage || product.imageUrl || product.ImageUrl || "";
        if (qImgEl) {
            qImgEl.src = pImg.startsWith('http') ? pImg : `${BACKEND_BASE_URL}/${pImg}`;
        }

        const variants = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];
        const variantContainer = document.getElementById('q-variant-list');

        if (!variantContainer) return;

        if (variants.length === 0) {
            variantContainer.innerHTML = `<p class="text-muted" style="font-size: 13px; color: #999;">Sản phẩm hiện đang tạm hết hàng.</p>`;
            return;
        }

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

            return `
                <button type="button" 
                        class="v-option-btn ${isOutOfStock ? 'disabled-stock-btn' : ''}" 
                        data-id="${vId}" 
                        ${isOutOfStock ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}
                        onclick="selectProductVariant(this, '${encodeURIComponent(JSON.stringify(v))}')">
                    ${displayLabel} <span style="font-size: 11px; color: #777;">${stockInfo}</span>
                </button>
            `;
        }).join('');

        const modalEl = document.getElementById('quick-variant-modal');
        if (modalEl) modalEl.style.display = 'flex';

        const submitBtn = document.getElementById('q-modal-submit-btn');
        if (submitBtn) submitBtn.onclick = handleQuickModalSubmit;

    } catch (err) {
        console.error("Lỗi khi mở popup phân loại:", err);
    }
}

function closeQuickVariantModal() {
    const modalEl = document.getElementById('quick-variant-modal');
    if (modalEl) modalEl.style.display = 'none';
}

function selectProductVariant(buttonElement, encodedVariantStr) {
    const buttons = document.querySelectorAll('.v-option-btn');
    buttons.forEach(btn => btn.classList.remove('selected'));

    buttonElement.classList.add('selected');
    selectedVariantObj = JSON.parse(decodeURIComponent(encodedVariantStr));
}

// 🌟 Xử lý nút XÁC NHẬN qua API (Thay thế hoàn toàn LocalStorage)
async function handleQuickModalSubmit() {
    const qtyInput = document.getElementById('q-modal-qty');
    const qty = qtyInput ? (parseInt(qtyInput.value) || 1) : 1;

    if (!selectedVariantObj || !currentProductData) {
        showModal("Thông báo", "Vui lòng chọn một phân loại sản phẩm trước khi xác nhận!");
        return;
    }

    const vId = selectedVariantObj.variantId || selectedVariantObj.VariantId || selectedVariantObj.id || 0;
    const token = localStorage.getItem("accessToken"); // Lấy JWT Token của khách hàng đã đăng nhập

    try {
        // Gửi yêu cầu thêm giỏ hàng lên ASP.NET Core API
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart/add`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}` // Truyền Token để xác thực UserId
            },
            body: JSON.stringify({ variantId: vId, quantity: qty })
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            showModal("Lỗi hệ thống", result.message || "Không thể thêm sản phẩm vào giỏ hàng. Vui lòng đăng nhập!");
            return;
        }

        // Cập nhật số hiển thị badge trên Header dựa vào dữ liệu thực tế từ DTO Server trả về
        updateCartBadge(result.data.totalItems);

        if (currentActionType === 'add_to_cart') {
            if (typeof showSuccessToast === 'function') showSuccessToast();
            closeQuickVariantModal();
        }
        else if (currentActionType === 'buy_now') {
            // Đánh dấu biến thể vừa chọn mua ngay để khi sang giohang.html tự động tích chọn
            localStorage.setItem('auto_check_item', JSON.stringify({ variantId: vId }));

            if (typeof showSuccessToast === 'function') showSuccessToast();
            setTimeout(() => { window.location.href = 'giohang.html'; }, 700);
        }
    } catch (err) {
        console.error("Lỗi API thêm vào giỏ:", err);
        showModal("Lỗi kết nối", "Không thể kết nối tới máy chủ!");
    }
}

//=========================================================================
// ── KHỐI LOGIC GIỎ HÀNG ĐỒNG BỘ VỚI DATABASE (API) ──
//=========================================================================
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

    const colorText = selectedColor.innerText;
    const sizeText = selectedSize.innerText;
    const variants = product.productVariants || product.ProductVariants || product.variants || product.Variants || [];

    const matchedVariant = variants.find(v =>
        (v.color || v.Color || '').toLowerCase() === colorText.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === sizeText.toLowerCase()
    );

    const vId = matchedVariant ? (matchedVariant.variantId || matchedVariant.VariantId || matchedVariant.id || 0) : 0;
    const token = localStorage.getItem("accessToken");

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart/add`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify({ variantId: vId, quantity: quantity })
        });

        const result = await response.json();
        if (!response.ok || !result.success) {
            showModal("Thông báo", result.message || "Vui lòng đăng nhập để thực hiện tính năng này!");
            return;
        }

        showSuccessToast();
        updateCartBadge(result.data.totalItems);
        setTimeout(() => { window.location.href = "giohang.html"; }, 800);
    } catch (err) {
        console.error(err);
    }
}

// 🌟 Đọc danh sách giỏ hàng từ API thay thế LocalStorage
async function renderCart() {
    const container = document.getElementById('cart-items-list');
    const templateEl = document.getElementById('cart-item-template');
    if (!container || !templateEl) return;

    const token = localStorage.getItem("accessToken");

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart`, {
            method: "GET",
            headers: { "Authorization": `Bearer ${token}` }
        });

        const result = await response.json();

        // Nếu không đăng nhập hoặc giỏ trống
        if (!response.ok || !result.success || !result.data || !result.data.items || result.data.items.length === 0) {
            container.innerHTML = `<div style="text-align:center; padding:50px;">Giỏ hàng của bạn đang trống</div>`;
            const totalAmountEl = document.getElementById('cart-total-amount');
            if (totalAmountEl) totalAmountEl.innerText = "0đ";

            // Cập nhật badge về 0 nếu giỏ hàng trống
            const badge = document.getElementById('cart-badge') || document.querySelector('.cart-count');
            if (badge) badge.innerText = "0";
            return;
        }

        // Lưu mảng items của DTO Server trả về vào window để các hàm phụ (update, delete) dễ dàng tra cứu dữ liệu gốc
        window.currentCartItems = result.data.items;

        const templateHtml = templateEl.innerHTML;

        container.innerHTML = result.data.items.map((item, index) => {
            const itemDiscount = Number(item.discount || 0);

            // Đảm bảo lấy đúng trường Giá gốc (unitPrice hoặc Price) từ API
            const price = Number(item.unitPrice || item.price || 0);
            const originalSubtotal = price * item.quantity;

            // Xử lý lấy ảnh chính xác từ API
            const rawImg = item.imageUrl || item.productImage || "";
            const itemImg = rawImg.startsWith("http") ? rawImg : `${BACKEND_BASE_URL}/${rawImg}`;

            const discountRow = itemDiscount > 0
                ? `<span style="text-decoration:line-through; color:#aaa; font-size:12px; display:block;">${formatVND(originalSubtotal)}</span>`
                : '';

            // 🌟 ĐỒNG BỘ THẾ NHÃN CHUẨN XÁC VỚI FILE HTML CỦA BẠN:
            return templateHtml
                .replaceAll('{index}', index)
                // Thay thế chuẩn xác nhãn {cartItemId} có trong file HTML bằng item.cartId từ API trả về
                .replaceAll('{cartItemId}', item.cartId)
                .replaceAll('{productId}', item.productId)
                .replaceAll('{variantId}', item.variantId)
                .replace('{itemImg}', itemImg)
                .replaceAll('{name}', item.productName || item.name)
                .replace('{color}', item.color || 'Mặc định')
                .replace('{size}', item.size || 'Freesize')
                .replace('{quantity}', item.quantity)
                .replace('{discountRow}', discountRow)
                .replace('{subtotal}', formatVND(item.subtotal)); // Sử dụng subtotal đã trừ giảm giá chuẩn từ C#
        }).join('');

        // Cập nhật số lượng lên biểu tượng giỏ hàng ở Header
        const badge = document.getElementById('cart-badge') || document.querySelector('.cart-count');
        if (badge) {
            badge.innerText = result.data.totalItems || result.data.items.reduce((sum, i) => sum + i.quantity, 0);
        }

        initCheckAllEvents();
        const checkAllTop = document.getElementById('check-all-top');
        const checkAllBottom = document.getElementById('check-all-bottom');
        if (checkAllTop) checkAllTop.checked = false;
        if (checkAllBottom) checkAllBottom.checked = false;

        handleAutoCheckBuyNow();
    } catch (err) {
        console.error("Lỗi khi tải giỏ hàng từ API:", err);
        container.innerHTML = `<div style="text-align:center; padding:50px; color:red;">Không thể kết nối dữ liệu giỏ hàng!</div>`;
    }
}
// 🌟 Cập nhật số lượng qua API
async function updateCartQty(cartItemId, change) {
    if (!cartItemId) return;

    // Tìm dữ liệu gốc của dòng sản phẩm này trong mảng window lưu trữ tạm thời lúc render
    const item = window.currentCartItems.find(i => i.cartId === cartItemId);
    if (!item) return;

    const targetQty = item.quantity + change;

    // Nếu giảm về số lượng <= 0, tự động gọi hàm xóa không thông báo
    if (targetQty <= 0) {
        await removeFromCart(cartItemId);
        return;
    }

    const token = localStorage.getItem('accessToken');
    if (!token) return;

    try {
        // Gửi thông tin cập nhật lên Endpoint PUT /api/cart/update
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart/update`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify({
                CartId: cartItemId, // Khớp chuẩn UpdateCartDto (Mã dòng sản phẩm cần sửa)
                Quantity: targetQty // Số lượng mới
            })
        });

        const result = await response.json();

        if (response.ok && result.success) {
            // Tải lại dữ liệu giỏ hàng bằng hàm renderCart vừa sửa để UI cập nhật giá tiền mới ngầm
            await renderCart();
        } else {
            alert(result.message || "Cập nhật số lượng thất bại. Có thể đã vượt quá số tồn kho của cửa hàng!");
        }
    } catch (error) {
        console.error("Lỗi cập nhật số lượng:", error);
    }
}

// 🌟 Xóa sản phẩm khỏi giỏ hàng qua API
async function removeFromCart(cartItemId) {
    // Chặn an toàn nếu không tìm thấy ID sản phẩm
    if (!cartItemId) return;

    const token = localStorage.getItem('accessToken');
    if (!token) {
        window.location.href = "login.html";
        return;
    }
    try {
        // Gửi request DELETE lên đúng endpoint của Backend
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart/${cartItemId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            console.error(`Lỗi từ server khi xóa sản phẩm: ${response.status}`);
            if (response.status === 401) window.location.href = "login.html";
            return;
        }

        const result = await response.json();

        if (result.success || result) {

            // 1. Cập nhật lại số lượng hiển thị trên Badge icon của Header ngay lập tức
            if (typeof updateCartBadge === "function") {
                await updateCartBadge();
            }

            // 2. Tải lại danh sách giỏ hàng mới từ Database để cập nhật giao diện người dùng
            if (typeof loadCartFromServer === "function") {
                loadCartFromServer(); // Tải lại dữ liệu bất đồng bộ mà không mất trải nghiệm người dùng
            } else if (typeof renderCart === "function") {
                renderCart();
            } else {
                window.location.reload(); // Cách fallback nếu bạn không có hàm load động
            }
        }
    } catch (error) {
        // Ghi nhận lỗi ngầm ở Console để lập trình viên theo dõi chứ không làm phiền khách hàng
        console.error("Lỗi xảy ra trong quá trình xóa mục giỏ hàng:", error);
    }
}

// Hàm cập nhật Badge số lượng hiển thị trên Header toàn cục
async function updateCartBadge(count) {
    const badge = document.getElementById('cart-count');
    if (!badge) return; // Chặn an toàn nếu trang đó không có navbar giỏ hàng

    // Trường hợp 1: Nếu có truyền số trực tiếp (ví dụ từ hàm renderCart hoặc khi cộng/trừ nhanh)
    if (count !== undefined && count !== null) {
        badge.innerText = count <= 0 ? "0" : (count > 9 ? "9+" : count);
        return;
    }

    // Trường hợp 2: Tự động chạy khi tải trang (Dành cho trang chủ, trang chi tiết, tài khoản...)
    const token = localStorage.getItem("accessToken");

    // Nếu chưa đăng nhập thì giỏ hàng mặc định bằng 0
    if (!token) {
        badge.innerText = "0";
        return;
    }

    try {
        // Gọi API ngầm lên Server lấy thông tin giỏ hàng thực tế của User
        const response = await fetch(`${BACKEND_BASE_URL}/api/cart`, {
            method: "GET",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Accept": "application/json"
            }
        });

        const result = await response.json();

        if (response.ok && result.success && result.data) {
            // Lấy trường tổng số lượng (totalItems) từ DTO của Backend trả về
            const total = result.data.totalItems;

            if (total !== undefined && total !== null) {
                badge.innerText = total <= 0 ? "0" : (total > 9 ? "9+" : total);
            } else {
                // Phương án dự phòng tính tổng quantity nếu tầng C# quên map trường totalItems
                const items = result.data.items || [];
                const fallbackTotal = items.reduce((sum, item) => sum + Number(item.quantity || 0), 0);
                badge.innerText = fallbackTotal <= 0 ? "0" : (fallbackTotal > 9 ? "9+" : fallbackTotal);
            }
        }
    } catch (err) {
        console.error("Lỗi tự động đồng bộ số lượng badge giỏ hàng:", err);
        badge.innerText = "0";
    }
}
document.addEventListener('DOMContentLoaded', () => {
    updateCartBadge();
});

function handleAutoCheckBuyNow() {
    const autoCheckData = localStorage.getItem('auto_check_item');
    if (!autoCheckData) return;

    const target = JSON.parse(autoCheckData);

    setTimeout(() => {
        const checkboxes = document.querySelectorAll('.cart-item-checkbox');
        let isItemFoundAndChecked = false;

        checkboxes.forEach(checkbox => {
            const vId = checkbox.getAttribute('data-variant-id');

            if (String(vId) === String(target.variantId)) {
                checkbox.checked = true;
                isItemFoundAndChecked = true;
            }
        });

        if (isItemFoundAndChecked) {
            localStorage.removeItem('auto_check_item');
            if (typeof updateTotalPrice === 'function') updateTotalPrice();
        }
    }, 200);
}

function initCheckAllEvents() {
    const checkAllTop = document.getElementById('check-all-top');
    const checkAllBottom = document.getElementById('check-all-bottom');

    if (!checkAllTop || !checkAllBottom) return;

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
    itemCheckboxes.forEach(cb => { cb.checked = status; });
    if (typeof updateTotalPrice === 'function') updateTotalPrice();
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

// 🌟 Biến toàn cục lưu trữ voucher đang được áp dụng ngoài Client
let appliedVoucher = null;

function updateTotalPrice() {
    if (!window.currentCartItems) return;
    const rows = document.querySelectorAll('.cart-item-row');
    const totalAmountEl = document.getElementById('cart-total-amount');

    let totalSelected = 0;

    rows.forEach(row => {
        const checkbox = row.querySelector('.item-checkbox');
        if (checkbox && checkbox.checked) {
            const cartItemId = checkbox.getAttribute('data-cart-item-id');
            const item = window.currentCartItems.find(i => (i.cartId == cartItemId || i.id == cartItemId));
            if (item) {
                totalSelected += item.subtotal;
            }
        }
    });

    let discountAmount = 0;

    if (appliedVoucher) {
        if (totalSelected < appliedVoucher.minOrderAmount) {
            showModal(
                "Khuyến mãi gỡ bỏ",
                `Tổng giá trị đơn hàng không còn đủ điều kiện áp dụng mã [${appliedVoucher.code}] (Yêu cầu đơn tối thiểu từ ${formatVND(appliedVoucher.minOrderAmount)}). Hệ thống đã gỡ bỏ mã.`
            );
            appliedVoucher = null;

            const voucherTrigger = document.querySelector('.select-voucher-trigger') || document.getElementById('open-voucher-btn');
            if (voucherTrigger) voucherTrigger.innerHTML = `Chọn hoặc nhập mã <i class="fa-solid fa-chevron-right"></i>`;
        } else {
            if (appliedVoucher.discountType === "Percent") {
                discountAmount = totalSelected * (appliedVoucher.discountValue / 100);
            } else {
                discountAmount = appliedVoucher.discountValue;
            }
        }
    }

    let finalPay = totalSelected - discountAmount;
    if (finalPay < 0) finalPay = 0;

    if (totalAmountEl) totalAmountEl.innerText = formatVND(finalPay);
    checkAndSyncMasterCheckbox();
}


// Hàm xử lý khi nhấn "Mua hàng" ở trang giỏ hàng
async function checkout() {
    const selectedItems = [];
    const checkboxes = document.querySelectorAll('.cart-item-checkbox:checked');

    if (checkboxes.length === 0) {
        showModal("Thông báo", "Vui lòng chọn ít nhất một sản phẩm!");
        return;
    }

    checkboxes.forEach(cb => {
        const cartItemId = cb.getAttribute('data-cart-item-id');
        // Tìm object item tương ứng trong window.currentCartItems để lấy quantity chuẩn từ API
        const item = window.currentCartItems.find(i => i.cartId == cartItemId);

        if (item) {
            selectedItems.push({
                cartItemId: item.cartId,
                productId: item.productId,
                variantId: item.variantId,
                quantity: item.quantity, 
                subtotal: item.subtotal  
            });
        }
    });

    // Lưu mảng này vào LocalStorage
    localStorage.setItem("checkout_data", JSON.stringify(selectedItems));
    window.location.href = "checkout.html";
}

//======================================================================
// VOUCHER / PROMOTION SYSTEM (API TRUY VẤN ĐỒNG BỘ TIỀN HÀNG)
//======================================================================
async function openVoucherModal() {
    const modal = document.getElementById('voucher-modal');
    if (!modal) return;

    modal.style.display = 'flex';

    let currentTotal = 0;
    if (window.currentCartItems) {
        const rows = document.querySelectorAll('.cart-item-row');
        rows.forEach(row => {
            const checkbox = row.querySelector('.item-checkbox');
            if (checkbox && checkbox.checked) {
                const cartItemId = checkbox.getAttribute('data-cart-item-id');
                const item = window.currentCartItems.find(i => (i.cartId == cartItemId || i.id == cartItemId));
                if (item) currentTotal += item.subtotal;
            }
        });
    }

    let voucherList = [];
    try {
        voucherList = await fetchData("promotion") || [];
    } catch (err) {
        console.error("Lỗi lấy danh sách mã giảm giá:", err);
    }

    const container = document.getElementById('modal-voucher-list');
    if (!container) return;

    if (voucherList.length === 0) {
        container.innerHTML = `<p style="text-align:center; padding: 20px; color: #888;">Hiện tại hệ thống chưa có mã giảm giá nào từ shop.</p>`;
        return;
    }

    container.innerHTML = voucherList.map(voucher => {
        const code = voucher.code || voucher.Code;
        const type = voucher.discountType || voucher.DiscountType;
        const value = Number(voucher.discountValue ?? voucher.DiscountValue ?? 0);
        const minOrder = Number(voucher.minOrderAmount ?? voucher.MinOrderAmount ?? 0);

        const isEligible = currentTotal >= minOrder;
        const textDiscount = type === "Percent" ? `Giảm ${value}%` : `Giảm ${formatVND(value)}`;
        const textCondition = `Đơn tối thiểu từ ${formatVND(minOrder)}`;

        return `
            <div class="voucher-card-item ${!isEligible ? 'voucher-disabled' : ''}" 
                 style="display: flex; align-items: center; border: 1px dashed #ff4757; padding: 12px; margin-bottom: 10px; border-radius: 6px; background: #fff5f6;">
                <div style="flex: 1;">
                    <div style="font-weight: bold; font-size: 15px; color: #2f3542;">Mã: ${code}</div>
                    <div style="color: #ff4757; font-weight: bold; margin: 4px 0;">${textDiscount}</div>
                    <div style="font-size: 12px; color: #747d8c;">${textCondition}</div>
                    ${!isEligible ? `<div style="font-size: 11px; color: #ffa502; font-weight: 500; margin-top: 4px;">Còn thiếu ${formatVND(minOrder - currentTotal)} để áp dụng</div>` : ''}
                </div>
                <div>
                    <button onclick="applyVoucherToCart('${code}', '${type}', ${value}, ${minOrder})"
                            ${!isEligible ? 'disabled' : ''} 
                            style="padding: 6px 14px; background: #2ed573; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">
                        Áp dụng
                    </button>
                </div>
            </div>
        `;
    }).join('');
}

function closeVoucherModal() {
    const modal = document.getElementById('voucher-modal');
    if (modal) modal.style.display = 'none';
}

function applyVoucherToCart(code, type, value, minOrderAmount) {
    const discountAmount = type === "Percent" ? (currentTotal * value / 100) : value;
    appliedVoucher = {
        code,
        discountType: type,
        discountValue: value,
        minOrderAmount,
        discountAmount 
    };
    localStorage.setItem("checkout_voucher", JSON.stringify(appliedVoucher));

    const voucherTrigger = document.querySelector('.select-voucher-trigger') || document.getElementById('open-voucher-btn');
    if (voucherTrigger) {
        voucherTrigger.innerHTML = `🎟️ Đang chọn mã: <b style="color:#2ed573;">${code}</b> (Đổi mã)`;
    }

    closeVoucherModal();
    updateTotalPrice();
}

async function applyManualVoucher() {
    const inputEl = document.getElementById('manual-voucher-code');
    if (!inputEl) return;
    const inputCode = inputEl.value.trim().toUpperCase();

    if (!inputCode) {
        showModal("Lỗi nhập liệu", "Vui lòng điền mã giảm giá cần áp dụng!");
        return;
    }

    let voucherList = [];
    try {
        voucherList = await fetchData("promotion") || [];
    } catch (e) {
        console.error(e);
    }

    const found = voucherList.find(v => (v.code || v.Code || '').toUpperCase() === inputCode);

    if (!found) {
        showModal("Lỗi áp dụng", "Mã giảm giá này không hợp lệ hoặc đã hết lượt sử dụng!");
        return;
    }

    const type = found.discountType || found.DiscountType;
    const value = Number(found.discountValue ?? found.DiscountValue ?? 0);
    const minOrder = Number(found.minOrderAmount ?? found.MinOrderAmount ?? 0);

    let currentTotal = 0;
    if (window.currentCartItems) {
        const rows = document.querySelectorAll('.cart-item-row');
        rows.forEach(row => {
            const checkbox = row.querySelector('.item-checkbox');
            if (checkbox && checkbox.checked) {
                const cartItemId = checkbox.getAttribute('data-cart-item-id');
                const item = window.currentCartItems.find(i => (i.cartId == cartItemId || i.id == cartItemId));
                if (item) currentTotal += item.subtotal;
            }
        });
    }

    if (currentTotal < minOrder) {
        showModal(
            "Chưa đạt điều kiện",
            `Mã giảm giá [${inputCode}] yêu cầu giá trị đơn hàng tối thiểu từ ${formatVND(minOrder)}. Đơn hàng hiện tại của bạn chưa đủ điều kiện!`
        );
        return;
    }

    applyVoucherToCart(inputCode, type, value, minOrder);
}





///////LOGIC CHUNG
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

function renderOptions(selector, data) {
    const container = document.querySelector(selector);
    if (!container) return;

    container.innerHTML = data.map(item =>
        `<button class="opt-btn" onclick="this.parentNode.querySelectorAll('.opt-btn').forEach(b=>b.classList.remove('active')); this.classList.add('active');">${item}</button>`
    ).join('');
}

function logout() {

    localStorage.removeItem("accessToken");
    localStorage.removeItem("checkout_voucher");
    window.location.href = "login.html";
}
