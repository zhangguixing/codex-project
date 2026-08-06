const api = require("../../utils/api");
Page({
  data: { user: null, activities: [], loading: true, loggingIn: false, endingId: null },
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
  },
  confirmEnd(event) {
    const id = event.detail.id;
    const activity = this.data.activities.find((item) => item.id === id);
    if (!activity || this.data.endingId) return;
    wx.showModal({
      title: "结束活动",
      content: activity.joinedPeople ? `结束后将停止报名，并通知 ${activity.joinedPeople} 位已报名玩家。` : "结束后将停止报名，活动记录会保留。",
      confirmText: "确认结束",
      confirmColor: "#c8472d",
      success: (result) => {
        if (!result.confirm) return;
        this.setData({ endingId: id });
        api.endActivity(id)
          .then(() => { wx.showToast({ title: "活动已结束", icon: "success" }); return this.load(); })
          .catch((error) => wx.showToast({ title: error.message, icon: "none" }))
          .finally(() => this.setData({ endingId: null }));
      }
    });
  }
});
