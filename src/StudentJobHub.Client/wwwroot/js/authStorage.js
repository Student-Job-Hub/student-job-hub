
window.authStorage = {
    setTokens: (token, refreshToken) => {
        localStorage.setItem("authToken", token);
        if (refreshToken) {
            localStorage.setItem("refreshToken", refreshToken);
        } else {
            localStorage.removeItem("refreshToken");
        }
    },
    getToken: () => localStorage.getItem("authToken"),
    getRefreshToken: () => localStorage.getItem("refreshToken"),
    removeToken: () => {
        localStorage.removeItem("authToken");
        localStorage.removeItem("refreshToken");
    }
};