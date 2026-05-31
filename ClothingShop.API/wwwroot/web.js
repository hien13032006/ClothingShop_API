const API_BASE_URL = "https://localhost:5001/api";

let products = [];
let mockVouchers = [];
let newProductsList = [];
let currentMoiIndex = 0;

// Hàm định dạng tiền tệ VND dùng chung
const formatVND = (amount) => {
    if (amount === undefined || amount === null) return 'Liên hệ';
    return amount.toLocaleString('vi-VN') + 'đ';
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

    mockVouchers = await fetchData("product/categories") || [];

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

    const prodsBanChay = bestSellersData ? (bestSellersData.items || bestSellersData.Items || bestSellersData) : [];
    const prodsKhuyenMai = discountsData ? (discountsData.items || discountsData.Items || discountsData) : [];
    newProductsList = newArrivalsData ? (newArrivalsData.items || newArrivalsData.Items || newArrivalsData) : [];

    const cleanBanChay = Array.isArray(prodsBanChay) ? prodsBanChay : [];
    const cleanKhuyenMai = Array.isArray(prodsKhuyenMai) ? prodsKhuyenMai : [];
    const cleanNewArrivals = Array.isArray(newProductsList) ? newProductsList : [];

    renderProducts('list-banchay', cleanBanChay);
    renderProducts('list-khuyenmai', cleanKhuyenMai);

    if (cleanNewArrivals.length > 0) {
        newProductsList = cleanNewArrivals;
        renderProductShowcase('showcase-container', newProductsList[currentMoiIndex]);
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
        const pId = item.productId || item.ProductId;
        const pName = item.name || item.Name || "Sản phẩm không tên";
        const pPrice = item.price !== undefined ? item.price : item.Price; // Gốc
        const pDiscount = item.discount !== undefined ? item.discount : item.Discount; // % Giảm
        const pMainImage = item.mainImage || item.MainImage || 'images/default.jpg';

        // Khởi tạo logic tính toán giá khuyến mãi trực tiếp từ discount
        const hasDiscount = pDiscount && pDiscount > 0;
        const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

        let discountBadgeHTML = '';
        let oldPriceHTML = '';

        if (hasDiscount) {
            discountBadgeHTML = `<span class="discount-badge" style="background-color:red; color:white; padding:2px 5px; position:absolute; z-index:2;">-${pDiscount}%</span>`;
            oldPriceHTML = `<span class="old-price" style="text-decoration: line-through; color: #999; margin-right: 5px; font-size: 13px;">${formatVND(pPrice)}</span>`;
        }

        return `
            <div class="product-card" style="position:relative;">
                <div class="cart-icon" onclick="quickAddToCart(${pId})"><i class="fa-solid fa-cart-plus"></i></div>
                <a href="chitietsp.html?id=${pId}" class="product-card-link" style="text-decoration: none; color: inherit;">
                    ${discountBadgeHTML}
                    <img src="${pMainImage}" alt="${pName}" onerror="this.src='images/default.jpg'">
                    <div class="product-info">
                        <p class="product-name">${pName}</p>
                        <div class="product-bottom">
                            <div class="price-box">
                                ${oldPriceHTML}
                                <span class="price" style="color: red; font-weight: bold;">${formatVND(finalPrice)}</span>
                            </div>
                            <button class="buy-btn">Mua ngay</button>
                        </div>
                    </div>
                </a>
            </div>
        `;
    }).join('');
}

function renderProductShowcase(containerId, product) {
    const container = document.getElementById(containerId);
    if (!container || !product) return;

    const pId = product.productId || product.ProductId;
    const pName = product.name || product.Name;
    const pPrice = product.price !== undefined ? product.price : product.Price;
    const pDiscount = product.discount !== undefined ? product.discount : product.Discount;
    const pDesc = product.description || product.Description || 'Đang cập nhật mô tả...';
    const pMainImage = product.mainImage || product.MainImage || 'images/default.jpg';

    const hasDiscount = pDiscount && pDiscount > 0;
    const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

    container.innerHTML = `
        <div class="product-image-section">
           <a href="chitietsp.html?id=${pId}">
                <img src="${pMainImage}" alt="${pName}" class="main-product-img" onerror="this.src='images/default.jpg'">
            </a>
        </div>
        <div class="product-info-section">
            <a href="chitietsp.html?id=${pId}" style="text-decoration: none; color: inherit;">
                <h3 class="product-title-highlight">${pName}</h3>
            </a>
            <div class="product-description">
                <h4>Mô tả sản phẩm mới</h4>
                <p>${pDesc}</p>
            </div>
            <div class="product-actions">
                <div style="display:flex; flex-direction:column;">
                    ${hasDiscount ? `<span style="text-decoration: line-through; color: #999; font-size:14px;">${formatVND(pPrice)} (-${pDiscount}%)</span>` : ''}
                    <span class="price" style="font-size: 20px; color: red; font-weight: bold;">${formatVND(finalPrice)}</span>
                </div>
                <button class="buy-now-btn" onclick="window.location.href='chitietsp.html?id=${pId}'">Mua ngay</button>
                <button class="add-to-cart-btn" onclick="quickAddToCart(${pId})"><i class="fa-solid fa-cart-plus"></i></button>
            </div>
        </div>
        <button class="slider-btn prev" onclick="moveMoiShowcase(-1)">&#10094;</button>
        <button class="slider-btn next" onclick="moveMoiShowcase(1)">&#10095;</button>
    `;
}

function renderVouchers(categories) {
    const container = document.getElementById('voucher-list');
    if (!container) return;

    container.innerHTML = categories.map(cat => {
        const catName = typeof cat === 'object' ? (cat.name || cat.Name) : cat;
        return `
            <div class="voucher-card" style="cursor: pointer;" onclick="window.location.href='danhmuc.html?cat=${encodeURIComponent(catName)}'">
                <div class="voucher-left">STYLE <br> CLOTH</div>
                <div class="voucher-right">
                    <p class="voucher-desc">${catName}</p> 
                    <button class="btn-save">Xem mục</button>
                </div>
            </div>
        `;
    }).join('');
}

function moveMoiShowcase(direction) {
    if (!newProductsList || newProductsList.length <= 1) return;

    currentMoiIndex += direction;
    if (currentMoiIndex >= newProductsList.length) currentMoiIndex = 0;
    if (currentMoiIndex < 0) currentMoiIndex = newProductsList.length - 1;

    renderProductShowcase('showcase-container', newProductsList[currentMoiIndex]);
}

// ── 5. LOGIC TRANG DANH MỤC SẢN PHẨM ─────────────────────────────────────
function initProductPages() {
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
            if (newProductsList.length > 0) {
                renderProductShowcase('showcase-container', newProductsList[currentMoiIndex]);
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
                const soldA = a.soldCount !== undefined ? a.soldCount : (a.SoldCount || 0);
                const soldB = b.soldCount !== undefined ? b.soldCount : (b.SoldCount || 0);
                return soldB - soldA;
            });
            if (titleElement) titleElement.innerText = "Sản phẩm bán chạy";
        } else if (type === 'khuyenmai') {
            filtered = filtered.filter(p => {
                const disc = p.discount !== undefined ? p.discount : p.Discount;
                return disc !== null && disc > 0;
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

// ── 6. TRANG CHI TIẾT SẢN PHẨM CHUẨN ĐẦU RA API ───────────────────────────
async function initDetailPage() {
    const params = new URLSearchParams(window.location.search);
    const productId = parseInt(params.get('id'));
    if (!productId) return;

    const product = await fetchData(`product/${productId}`);

    if (product) {
        const pName = product.name || product.Name;
        const pPrice = product.price !== undefined ? product.price : product.Price;
        const pDiscount = product.discount !== undefined ? product.discount : product.Discount;
        const pMainImg = product.mainImage || product.MainImage || 'images/default.jpg';
        const pThumbnails = product.thumbnails || product.Thumbnails || [];
        const pDesc = product.description || product.Description || 'Chưa có thông tin mô tả cụ thể.';
        const pPolicy = product.policy || product.Policy;

        const hasDiscount = pDiscount && pDiscount > 0;
        const finalPrice = hasDiscount ? pPrice * (1 - pDiscount / 100) : pPrice;

        const mainImg = document.getElementById('main-img');
        if (mainImg) mainImg.src = pMainImg;

        const thumbContainer = document.querySelector('.thumbnail-list');
        if (thumbContainer && pThumbnails.length > 0) {
            thumbContainer.innerHTML = pThumbnails.map(t =>
                `<img src="${t}" onclick="document.getElementById('main-img').src=this.src" alt="thumb">`
            ).join('');
        } else if (thumbContainer) {
            thumbContainer.innerHTML = `<img src="${pMainImg}" onclick="document.getElementById('main-img').src=this.src" alt="thumb">`;
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

        renderRatingSection(product);

        const colorSet = new Set();
        const sizeSet = new Set();
        const variants = product.variants || product.Variants;
        if (variants) {
            variants.forEach(v => {
                const c = v.color || v.Color;
                const s = v.size || v.Size;
                if (c) colorSet.add(c);
                if (s) sizeSet.add(s);
            });
        }
        renderOptions('#color-options', Array.from(colorSet).length > 0 ? Array.from(colorSet) : ["Mặc định"]);
        renderOptions('#size-options', Array.from(sizeSet).length > 0 ? Array.from(sizeSet) : ["Freesize"]);

        const descBox = document.getElementById('p-desc');
        if (descBox) {
            let infoHtml = `<p>${pDesc}</p>`;
            if (pPolicy) {
                infoHtml += `<h4 style="margin-top: 15px;">Chính sách cửa hàng</h4><p>${pPolicy}</p>`;
            }
            descBox.innerHTML = infoHtml;
        }
    } else {
        console.error("Không tìm thấy thông tin chi tiết của sản phẩm này.");
    }
}

function renderOptions(selector, data) {
    const container = document.querySelector(selector);
    if (!container) return;

    const isColor = selector.includes('color');
    const type = isColor ? 'color' : 'size';
    const errorText = isColor ? 'Vui lòng chọn màu sắc' : 'Vui lòng chọn kích thước';

    container.innerHTML = data.map(item =>
        `<button class="opt-btn" onclick="selectOption(this)">${item}</button>`
    ).join('') + `<div id="${type}-error" class="error-msg" style="display:none; color:red; font-size:12px; margin-top:5px; font-style:italic;">${errorText}</div>`;
}

function selectOption(btn) {
    const parent = btn.parentElement;
    parent.querySelectorAll('.opt-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');

    const errorMsg = parent.querySelector('.error-msg');
    if (errorMsg) errorMsg.style.display = 'none';
}

function renderRatingSection(p) {
    const container = document.querySelector('.rating-sold');
    if (!container) return;

    const ratingVal = p.averageRating || p.AverageRating || 5.0;
    const rCount = p.reviewCount !== undefined ? p.reviewCount : (p.ReviewCount || 0);
    const sCount = p.soldCount !== undefined ? p.soldCount : (p.SoldCount || 0);

    const starsHTML = generateStars(ratingVal);
    const soldFormatted = sCount >= 1000 ? (sCount / 1000).toFixed(1) + 'K' : sCount;

    container.innerHTML = `
        <span class="rating-num">${ratingVal}</span>
        <div class="stars">${starsHTML}</div>
        <span class="divider">|</span>
        <span class="reviews">${rCount} Đánh giá</span>
        <span class="divider">|</span>
        <span class="sold">${soldFormatted} Đã bán</span>
    `;
}

function generateStars(rating) {
    let html = '';
    for (let i = 1; i <= 5; i++) {
        if (i <= Math.floor(rating)) {
            html += '<i class="fa-solid fa-star" style="color: #ffcc00;"></i>';
        } else if (i - 0.5 <= rating) {
            html += '<i class="fa-solid fa-star-half-stroke" style="color: #ffcc00;"></i>';
        } else {
            html += '<i class="fa-regular fa-star" style="color: #ccc;"></i>';
        }
    }
    return html;
}

// ── 7. KHỐI LOGIC GIỎ HÀNG (CẬP NHẬT ĐỒNG BỘ VARIANTID VÀ DISCOUNT) ──────────
async function addToCart() {
    const params = new URLSearchParams(window.location.search);
    const productId = parseInt(params.get('id'));

    const product = await fetchData(`product/${productId}`);
    if (!product) return;

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

    const pId = product.productId || product.ProductId;
    const pName = product.name || product.Name;
    const pPrice = product.price !== undefined ? product.price : product.Price; // Gốc
    const pDiscount = product.discount !== undefined ? product.discount : product.Discount;
    const pMainImg = product.mainImage || product.MainImage;

    const colorText = selectedColor.innerText;
    const sizeText = selectedSize.innerText;

    // Tìm kiếm VariantId thực tế từ cơ sở dữ liệu dựa vào Màu và Kích cỡ được chọn
    const variants = product.variants || product.Variants || [];
    const matchedVariant = variants.find(v =>
        (v.color || v.Color || '').toLowerCase() === colorText.toLowerCase() &&
        (v.size || v.Size || '').toLowerCase() === sizeText.toLowerCase()
    );

    // Nếu không khớp (Sản phẩm Mặc định/Freesize), gán giá trị mặc định đầu tiên hoặc fallback 0
    const vId = matchedVariant ? (matchedVariant.variantId || matchedVariant.VariantId) : (variants[0]?.variantId || 0);

    const cartItem = {
        productId: pId,
        variantId: vId, // ĐỒNG BỘ THÊM TRƯỜNG VARIANTID
        name: pName,
        price: pPrice, // Lưu gốc
        discount: pDiscount || 0, // ĐỒNG BỘ THÊM TRƯỜNG DISCOUNT
        mainImage: pMainImg,
        color: colorText,
        size: sizeText,
        quantity: quantity
    };

    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    // Tìm kiếm phần tử trùng cả ProductId và VariantId
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
    const product = await fetchData(`product/${productId}`);
    if (!product) return;

    const pId = product.productId || product.ProductId;
    const pName = product.name || product.Name;
    const pPrice = product.price !== undefined ? product.price : product.Price;
    const pDiscount = product.discount !== undefined ? product.discount : product.Discount;
    const pMainImg = product.mainImage || product.MainImage;

    const variants = product.variants || product.Variants || [];
    // Tự động lấy biến thể đầu tiên khi mua nhanh tại danh sách sản phẩm
    const defaultVariant = variants[0];
    const vId = defaultVariant ? (defaultVariant.variantId || defaultVariant.VariantId) : 0;
    const vColor = defaultVariant ? (defaultVariant.color || defaultVariant.Color) : "Mặc định";
    const vSize = defaultVariant ? (defaultVariant.size || defaultVariant.Size) : "Freesize";

    const cartItem = {
        productId: pId,
        variantId: vId, // ĐỒNG BỘ VARIANTID
        name: pName,
        price: pPrice,
        discount: pDiscount || 0, // ĐỒNG BỘ DISCOUNT
        mainImage: pMainImg,
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
    if (!container) return;

    const cart = JSON.parse(localStorage.getItem('cart')) || [];

    if (cart.length === 0) {
        container.innerHTML = `<div style="text-align:center; padding:50px;">Giỏ hàng của bạn đang trống</div>`;
        const totalAmountEl = document.getElementById('cart-total-amount');
        if (totalAmountEl) totalAmountEl.innerText = "0đ";
        return;
    }

    container.innerHTML = cart.map((item, index) => {
        // Áp dụng công thức tính giá thực bán sau giảm giá phần trăm trực tiếp tại giỏ hàng
        const itemDiscount = item.discount || 0;
        const finalPrice = itemDiscount > 0 ? item.price * (1 - itemDiscount / 100) : item.price;
        const subtotal = finalPrice * item.quantity;

        const hasDiscount = itemDiscount > 0;

        return `
            <div class="cart-item-row" data-index="${index}">
                <div style="text-align: center;"><input type="checkbox" class="item-checkbox" onchange="updateTotalPrice()"></div>
                <div class="cart-item-info">
                    <img src="${item.mainImage || 'images/default.jpg'}" alt="${item.name}">
                    <p>${item.name}</p>
                </div>
                <div class="cart-item-type">Màu: ${item.color} <br> Size: ${item.size}</div>
                <div class="qty-control">
                    <button onclick="updateCartQty(${index}, -1)">−</button>
                    <span>${item.quantity}</span>
                    <button onclick="updateCartQty(${index}, 1)">+</button>
                </div>
                <div class="cart-item-price">
                    ${hasDiscount ? `<span style="text-decoration:line-through; color:#aaa; font-size:12px; display:block;">${formatVND(item.price * item.quantity)}</span>` : ''}
                    <span class="actual-price" style="font-weight:bold; color:red;">${formatVND(subtotal)}</span>
                </div>
                <div class="cart-item-action"><span class="btn-delete" onclick="removeFromCart(${index})">Xóa</span></div>
            </div>
        `;
    }).join('');

    const totalAmountEl = document.getElementById('cart-total-amount');
    if (totalAmountEl) totalAmountEl.innerText = "0đ";
}

function updateCartQty(index, delta) {
    let cart = JSON.parse(localStorage.getItem('cart')) || [];
    if (cart[index].quantity + delta < 1) return;

    cart[index].quantity += delta;
    localStorage.setItem('cart', JSON.stringify(cart));

    const row = document.querySelector(`.cart-item-row[data-index="${index}"]`);
    if (row) {
        const qtySpan = row.querySelector('.qty-control span');
        if (qtySpan) qtySpan.innerText = cart[index].quantity;

        // Tính lại thành tiền dựa trên giá thực bán sau giảm giá phần trăm
        const item = cart[index];
        const itemDiscount = item.discount || 0;
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
                // Tính toán cộng dồn tổng đơn hàng qua giá thực bán (finalPrice)
                const itemDiscount = item.discount || 0;
                const finalPrice = itemDiscount > 0 ? item.price * (1 - itemDiscount / 100) : item.price;
                totalSelected += finalPrice * item.quantity;
            }
        }
    });
    if (totalAmountEl) totalAmountEl.innerText = formatVND(totalSelected);
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

function checkout() {
    const selectedCheckboxes = document.querySelectorAll('.item-checkbox:checked');
    if (selectedCheckboxes.length === 0) {
        showModal("Thông báo", "Vui lòng chọn ít nhất một sản phẩm để tiến hành thanh toán!");
        return;
    }
    const cart = JSON.parse(localStorage.getItem('cart')) || [];
    const selectedItems = [];
    selectedCheckboxes.forEach(checkbox => {
        const row = checkbox.closest('.cart-item-row');
        if (row) {
            const index = row.getAttribute('data-index');
            if (cart[index]) selectedItems.push(cart[index]);
        }
    });
    localStorage.setItem('checkoutItems', JSON.stringify(selectedItems));
    window.location.href = "thanhtoan.html";
}