// ===================================
// Configuration
// ===================================
const msalConfig = {
    auth: {
        clientId: "b932f9cd-e8d5-46ce-bd37-8a0f3aca4373", // Azure Client ID
        authority: "https://login.microsoftonline.com/6c01af9b-e68a-4616-bcc6-4685d9acd910", // Azure Tenant ID
        redirectUri: "http://localhost:3000", // 현재 페이지 URL
    },
    cache: {
        cacheLocation: "sessionStorage",
        storeAuthStateInCookie: false,
    }
};

const API_BASE_URL = 'http://localhost:5000/api';

// ===================================
// Token Manager Class
// ===================================
class TokenManager {
    constructor() {
        this.accessToken = localStorage.getItem('accessToken');
        this.refreshToken = localStorage.getItem('refreshToken');
        this.accessTokenExpiry = localStorage.getItem('accessTokenExpiry');
    }
    
    saveTokens(accessToken, refreshToken, expiresIn) {
        this.accessToken = accessToken;
        this.refreshToken = refreshToken;
        this.accessTokenExpiry = new Date(Date.now() + expiresIn * 1000).toISOString();
        
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('accessTokenExpiry', this.accessTokenExpiry);
    }
    
    clearTokens() {
        this.accessToken = null;
        this.refreshToken = null;
        this.accessTokenExpiry = null;
        
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('accessTokenExpiry');
    }
    
    isAccessTokenValid() {
        if (!this.accessToken || !this.accessTokenExpiry) return false;
        return new Date() < new Date(this.accessTokenExpiry);
    }
    
    async getValidAccessToken() {
        if (this.isAccessTokenValid()) {
            return this.accessToken;
        }
        
        if (this.refreshToken) {
            try {
                const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken: this.refreshToken })
                });
                
                if (response.ok) {
                    const data = await response.json();
                    this.saveTokens(data.accessToken, data.refreshToken, data.expiresIn);
                    updateTokenDisplay();
                    showSuccess('토큰이 자동으로 갱신되었습니다.');
                    return this.accessToken;
                }
            } catch (error) {
                console.error('Token refresh failed:', error);
            }
        }
        
        this.clearTokens();
        return null;
    }
    
    getTimeUntilExpiry() {
        if (!this.accessTokenExpiry) return null;
        const now = new Date();
        const expiry = new Date(this.accessTokenExpiry);
        const diff = expiry - now;
        
        if (diff <= 0) return null;
        
        const minutes = Math.floor(diff / 60000);
        const seconds = Math.floor((diff % 60000) / 1000);
        return { minutes, seconds, total: diff };
    }
}

// ===================================
// Initialize
// ===================================
const tokenManager = new TokenManager();
const msalInstance = new msal.PublicClientApplication(msalConfig);
let currentUser = null;
let tokenTimer = null;

// DOM Elements
const loginBtn = document.getElementById('login-btn');
const logoutBtn = document.getElementById('logout-btn');
const searchBtn = document.getElementById('search-btn');
const clearBtn = document.getElementById('clear-btn');
const searchInput = document.getElementById('search-input');
const refreshTokenBtn = document.getElementById('refresh-token-btn');
const userInfo = document.getElementById('user-info');
const loginSection = document.getElementById('login-section');
const searchSection = document.getElementById('search-section');
const tokenSection = document.getElementById('token-section');
const searchResults = document.getElementById('search-results');
const accessTokenTextarea = document.getElementById('access-token');
const refreshTokenTextarea = document.getElementById('refresh-token');
const tokenTimerDiv = document.getElementById('token-timer');

// ===================================
// Event Listeners
// ===================================
window.addEventListener('load', async () => {
    try {
        // Handle redirect response
        const response = await msalInstance.handleRedirectPromise();
        if (response) {
            await handleLoginResponse(response);
        }
        
        // Check if user is already logged in
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
            currentUser = accounts[0];
            
            // Check if we have valid tokens
            if (tokenManager.accessToken && tokenManager.refreshToken) {
                updateUI();
                await searchDocuments('');
                startTokenTimer();
            }
        }
    } catch (error) {
        console.error('Initialization error:', error);
    }
});

// Login button
loginBtn.addEventListener('click', async () => {
    try {
        const loginRequest = {
            scopes: ["openid", "profile", "email"],
        };
        
        try {
            const response = await msalInstance.loginPopup(loginRequest);
            await handleLoginResponse(response);
        } catch (popupError) {
            console.log('Popup blocked, using redirect');
            msalInstance.loginRedirect(loginRequest);
        }
    } catch (error) {
        console.error('Login error:', error);
        showError('로그인 실패: ' + error.message);
    }
});

// Logout button
logoutBtn.addEventListener('click', async () => {
    try {
        // Call server logout
        const token = await tokenManager.getValidAccessToken();
        if (token) {
            await fetch(`${API_BASE_URL}/auth/logout`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
        }
    } catch (error) {
        console.error('Server logout error:', error);
    }
    
    // Clear local tokens
    tokenManager.clearTokens();
    
    // MSAL logout
    const logoutRequest = {
        account: msalInstance.getAccountByUsername(currentUser.username),
        postLogoutRedirectUri: "http://localhost:3000"
    };
    
    msalInstance.logoutRedirect(logoutRequest);
});

// Search button
searchBtn.addEventListener('click', async () => {
    const query = searchInput.value;
    await searchDocuments(query);
});

// Clear button
clearBtn.addEventListener('click', () => {
    searchInput.value = '';
    searchDocuments('');
});

// Search input enter key
searchInput.addEventListener('keypress', async (e) => {
    if (e.key === 'Enter') {
        await searchDocuments(searchInput.value);
    }
});

// Tab buttons
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', (e) => {
        const tab = e.target.dataset.tab;
        
        // Update active tab button
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        e.target.classList.add('active');
        
        // Show corresponding content
        document.querySelectorAll('.tab-content').forEach(content => {
            content.style.display = 'none';
        });
        document.getElementById(`${tab}-tab`).style.display = 'block';
    });
});

// Decode buttons
document.getElementById('decode-access-btn').addEventListener('click', () => {
    decodeAndDisplayToken(tokenManager.accessToken, 'decoded-access-token');
});

document.getElementById('decode-refresh-btn').addEventListener('click', () => {
    decodeAndDisplayToken(tokenManager.refreshToken, 'decoded-refresh-token');
});

// Copy buttons
document.getElementById('copy-access-btn').addEventListener('click', () => {
    navigator.clipboard.writeText(tokenManager.accessToken);
    showSuccess('Access Token이 클립보드에 복사되었습니다.');
});

document.getElementById('copy-refresh-btn').addEventListener('click', () => {
    navigator.clipboard.writeText(tokenManager.refreshToken);
    showSuccess('Refresh Token이 클립보드에 복사되었습니다.');
});

// Refresh token button
refreshTokenBtn.addEventListener('click', async () => {
    const newToken = await tokenManager.getValidAccessToken();
    if (newToken) {
        showSuccess('토큰이 갱신되었습니다.');
        updateTokenDisplay();
        startTokenTimer();
    } else {
        showError('토큰 갱신 실패. 다시 로그인해주세요.');
    }
});

// ===================================
// Functions
// ===================================
async function handleLoginResponse(response) {
    try {
        // Send Azure token to backend
        const result = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                azureToken: response.accessToken
            })
        });
        
        if (!result.ok) {
            const error = await result.json();
            throw new Error(error.error || 'Server authentication failed');
        }
        
        const data = await result.json();
        tokenManager.saveTokens(data.accessToken, data.refreshToken, data.expiresIn);
        currentUser = response.account;
        
        updateUI();
        updateTokenDisplay();
        showSuccess('로그인 성공!');
        
        // Load documents
        await searchDocuments('');
        
        // Start token timer
        startTokenTimer();
        
    } catch (error) {
        console.error('Authentication error:', error);
        showError('인증 실패: ' + error.message);
    }
}

async function searchDocuments(query) {
    const token = await tokenManager.getValidAccessToken();
    if (!token) {
        showError('로그인이 필요합니다.');
        return;
    }
    
    try {
        searchResults.innerHTML = '<div class="loading">검색 중...</div>';
        
        const url = query 
            ? `${API_BASE_URL}/documents?search=${encodeURIComponent(query)}`
            : `${API_BASE_URL}/documents`;
            
        const response = await fetch(url, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                // Token expired, try refresh
                const newToken = await tokenManager.getValidAccessToken();
                if (newToken) {
                    return searchDocuments(query);
                }
            }
            throw new Error('Search failed');
        }
        
        const documents = await response.json();
        displayResults(documents);
        
    } catch (error) {
        console.error('Search error:', error);
        searchResults.innerHTML = '<div class="error">검색 실패: ' + error.message + '</div>';
    }
}

function displayResults(documents) {
    if (documents.length === 0) {
        searchResults.innerHTML = '<div class="loading">검색 결과가 없습니다.</div>';
        return;
    }
    
    searchResults.innerHTML = documents.map(doc => `
        <div class="document-item">
            <div class="document-title">${doc.title}</div>
            <span class="document-category">${doc.category}</span>
            <div class="document-content">${doc.content}</div>
        </div>
    `).join('');
}

function decodeAndDisplayToken(token, elementId) {
    if (!token) {
        showError('토큰이 없습니다.');
        return;
    }
    
    try {
        const parts = token.split('.');
        const header = JSON.parse(atob(parts[0]));
        const payload = JSON.parse(atob(parts[1]));
        
        const decoded = document.getElementById(elementId);
        decoded.innerHTML = `
<strong>📋 Header:</strong>
${JSON.stringify(header, null, 2)}

<strong>📦 Payload:</strong>
${JSON.stringify(payload, null, 2)}

<strong>⏰ Expires:</strong>
${new Date(payload.exp * 1000).toLocaleString('ko-KR')}

<strong>⏱️ Issued At:</strong>
${new Date(payload.iat * 1000).toLocaleString('ko-KR')}
        `;
    } catch (error) {
        document.getElementById(elementId).innerHTML = '<div class="error">토큰 디코딩 실패</div>';
    }
}

function updateTokenDisplay() {
    if (tokenManager.accessToken) {
        accessTokenTextarea.value = tokenManager.accessToken;
    } else {
        accessTokenTextarea.value = '';
    }
    
    if (tokenManager.refreshToken) {
        refreshTokenTextarea.value = tokenManager.refreshToken;
    } else {
        refreshTokenTextarea.value = '';
    }
}

function startTokenTimer() {
    // Clear existing timer
    if (tokenTimer) {
        clearInterval(tokenTimer);
    }
    
    // Update timer every second
    tokenTimer = setInterval(() => {
        const timeInfo = tokenManager.getTimeUntilExpiry();
        
        if (!timeInfo) {
            tokenTimerDiv.innerHTML = '<span class="error">토큰 만료됨</span>';
            refreshTokenBtn.style.display = 'inline-block';
            clearInterval(tokenTimer);
            return;
        }
        
        const { minutes, seconds } = timeInfo;
        
        if (minutes <= 5) {
            tokenTimerDiv.innerHTML = `<span class="warning">⚠️ 토큰 만료까지: ${minutes}분 ${seconds}초</span>`;
            refreshTokenBtn.style.display = 'inline-block';
        } else {
            tokenTimerDiv.innerHTML = `<span>✅ 토큰 만료까지: ${minutes}분 ${seconds}초</span>`;
            refreshTokenBtn.style.display = 'none';
        }
    }, 1000);
}

function updateUI() {
    if (currentUser && tokenManager.accessToken) {
        userInfo.innerHTML = `
            <div class="success">
                👤 로그인됨: ${currentUser.username}<br>
                📧 ${currentUser.name}
            </div>
        `;
        loginSection.style.display = 'none';
        searchSection.style.display = 'block';
        tokenSection.style.display = 'block';
        logoutBtn.style.display = 'inline-block';
    } else {
        userInfo.innerHTML = '';
        loginSection.style.display = 'block';
        searchSection.style.display = 'none';
        tokenSection.style.display = 'none';
        logoutBtn.style.display = 'none';
        
        if (tokenTimer) {
            clearInterval(tokenTimer);
        }
    }
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'error';
    errorDiv.textContent = '❌ ' + message;
    document.querySelector('main').prepend(errorDiv);
    setTimeout(() => errorDiv.remove(), 5000);
}

function showSuccess(message) {
    const successDiv = document.createElement('div');
    successDiv.className = 'success';
    successDiv.textContent = '✅ ' + message;
    document.querySelector('main').prepend(successDiv);
    setTimeout(() => successDiv.remove(), 5000);
}

function showWarning(message) {
    const warningDiv = document.createElement('div');
    warningDiv.className = 'warning';
    warningDiv.textContent = '⚠️ ' + message;
    document.querySelector('main').prepend(warningDiv);
    setTimeout(() => warningDiv.remove(), 5000);
}
