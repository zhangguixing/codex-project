const api = require("../../utils/api");

function cityFromAddress(address) {
  const match = (address || "").match(/(?:[^省]+省)?([^市]+市)/);
  return match ? match[1] : "";
}

Page({
  data: { activities: [], types: [], selectedType: "", timeRange: "", city: "", locationLabel: "定位中", loading: true, locating: false },
  onLoad() { this.locate(); api.gameTypes().then((types) => this.setData({ types })).catch(this.showError); },
  onShow() { const tabBar = this.getTabBar && this.getTabBar(); if (tabBar) tabBar.setSelected(0); },
  onPullDownRefresh() { this.load().finally(() => wx.stopPullDownRefresh()); },
  load() {
    const location = wx.getStorageSync("homeLocation") || {};
    this.setData({ loading: true });
    return api.activities({ city: this.data.city, gameTypeId: this.data.selectedType || undefined, timeRange: this.data.timeRange || undefined, latitude: location.latitude, longitude: location.longitude, sort: "latest" })
      .then((page) => this.setData({ activities: page.records || [] }))
      .catch(this.showError)
      .finally(() => this.setData({ loading: false }));
  },
  chooseType(e) { this.setData({ selectedType: e.currentTarget.dataset.id }, () => this.load()); },
  chooseTime(e) { this.setData({ timeRange: e.currentTarget.dataset.range }, () => this.load()); },
  createActivity() { wx.switchTab({ url: "/pages/create/index" }); },
  search(e) { this.setData({ city: e.detail.value }, () => this.load()); },
  locate() {
    this.setData({ locating: true, locationLabel: "定位中" });
    wx.getLocation({
      type: "gcj02",
      success: (location) => {
        wx.setStorageSync("homeLocation", { latitude: location.latitude, longitude: location.longitude });
        this.setData({ locationLabel: "已获取", locating: false }, () => this.load());
      },
      fail: () => {
        this.setData({ locationLabel: "请地图选择", locating: false }, () => this.load());
      }
    });
  },
  chooseLocation() {
    wx.chooseLocation({
      success: (location) => {
        const city = cityFromAddress(location.address) || this.data.city;
        wx.setStorageSync("homeLocation", { latitude: location.latitude, longitude: location.longitude });
        this.setData({ city, locationLabel: city || location.name || "已选择地图位置" }, () => this.load());
      },
      fail: (error) => {
        if (!/cancel/.test((error && error.errMsg) || "")) wx.showToast({ title: "地图位置选择失败", icon: "none" });
      }
    });
  },
  showError(error) { wx.showToast({ title: error.message || "加载失败", icon: "none" }); }
});
