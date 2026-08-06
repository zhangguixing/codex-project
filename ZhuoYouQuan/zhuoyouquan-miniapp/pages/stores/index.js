const api = require("../../utils/api");
Page({
  data: { stores: [], loading: true },
  onLoad() { this.load(); },
  load() { this.setData({ loading: true }); return api.stores().then((stores) => this.setData({ stores })).catch((error) => wx.showToast({ title: error.message, icon: "none" })).finally(() => this.setData({ loading: false })); },
  open(e) { wx.navigateTo({ url: `/pages/store-detail/index?id=${e.currentTarget.dataset.id}` }); },
  back() { wx.navigateBack(); }
});
