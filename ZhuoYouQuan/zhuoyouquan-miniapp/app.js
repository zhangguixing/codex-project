App({
  globalData: {
    user: null
  },
  ensureSession() {
    const { login } = require("./utils/api");
    if (this.globalData.user) return Promise.resolve(this.globalData.user);
    return login().then((user) => {
      this.globalData.user = user;
      return user;
    });
  },
  clearSession() {
    this.globalData.user = null;
    wx.removeStorageSync("token");
  },
  hasSession() {
    return Boolean(wx.getStorageSync("token"));
  }
});
