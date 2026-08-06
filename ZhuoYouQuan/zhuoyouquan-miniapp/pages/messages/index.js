const api = require("../../utils/api");

Page({
  data: { messages: [], loading: false },
  onShow() {
    const tabBar = this.getTabBar && this.getTabBar();
    if (tabBar) tabBar.setSelected(3);
    this.load();
  },
  load() {
    this.setData({ loading: true });
    getApp().ensureSession()
      .then(() => api.messages())
      .then((messages) => this.setData({ messages: messages || [] }))
      .catch((error) => wx.showToast({ title: error.message, icon: "none" }))
      .finally(() => this.setData({ loading: false }));
  },
  openActivity(event) {
    const id = event.currentTarget.dataset.id;
    if (id) wx.navigateTo({ url: `/pages/detail/index?id=${id}` });
  }
});
