const api = require("../../utils/api");
Page({
  data: { activities: [], loading: true },
  onShow() { const tabBar = this.getTabBar && this.getTabBar(); if (tabBar) tabBar.setSelected(1); this.load(); },
  load() { this.setData({ loading: true }); return api.activities({ sort: "latest" }).then((page) => this.setData({ activities: page.records || [] })).catch((e) => wx.showToast({ title: e.message, icon: "none" })).finally(() => this.setData({ loading: false })); }
});
