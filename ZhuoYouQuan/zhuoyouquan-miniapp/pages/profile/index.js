const api = require("../../utils/api");
Page({
  data: { user: null, activities: [], loading: true, loggingIn: false },
  onShow() { const tabBar = this.getTabBar && this.getTabBar(); if (tabBar) tabBar.setSelected(4); this.load(); },
  onPullDownRefresh() { this.load().finally(() => wx.stopPullDownRefresh()); },
  load() {
    if (!getApp().hasSession()) { this.setData({ user: null, activities: [], loading: false }); return Promise.resolve(); }
    this.setData({ loading: true });
    return Promise.all([api.profile(), api.myActivities("created")])
      .then(([user, page]) => { getApp().globalData.user = user; this.setData({ user, activities: page.records || [] }); })
      .catch((e) => {
        wx.removeStorageSync("token");
        getApp().globalData.user = null;
        this.setData({ user: null, activities: [] });
        wx.showToast({ title: "登录已失效，请点击头像重新授权", icon: "none" });
      })
      .finally(() => this.setData({ loading: false }));
  },
  login() {
    if (this.data.loggingIn) return;
    this.setData({ loggingIn: true });
    getApp().ensureSession().then(() => this.load()).catch(() => wx.showToast({ title: "登录未完成，请重试", icon: "none" })).finally(() => this.setData({ loggingIn: false }));
  }
});
