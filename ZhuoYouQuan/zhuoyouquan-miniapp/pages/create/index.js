const api = require("../../utils/api");
const { coverTheme } = require("../../utils/activity");

function tomorrow() {
  const day = new Date(Date.now() + 86400000);
  return `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, "0")}-${String(day.getDate()).padStart(2, "0")}`;
}

function cityFromAddress(address) {
  const match = (address || "").match(/(?:[^省]+省)?([^市]+市)/);
  return match ? match[1] : "";
}

Page({
  data: {
    types: [],
    typeIndex: -1,
    hasType: false,
    typeLabel: "\u8bf7\u9009\u62e9\u6e38\u620f\u7c7b\u578b",
    cityLabel: "\u8bf7\u9009\u62e9\u57ce\u5e02",
    editing: false,
    submitting: false,
    uploadingCover: false,
    coverTheme: "tabletop",
    form: { title: "", gameTypeId: null, coverUrl: "", date: tomorrow(), time: "19:30", maxPeople: "6", city: "", storeName: "", address: "", longitude: null, latitude: null, fee: "0", aa: true, newbieFriendly: true, durationMinutes: "120", difficulty: "轻松", language: "中文", teachingProvided: true, bringGame: true, description: "" }
  },
  onLoad() { api.gameTypes().then((types) => { this.setData({ types }); this.applyEditDraft(types); }).catch((e) => wx.showToast({ title: e.message, icon: "none" })); },
  onShow() {
    if (this.data.types.length) this.applyEditDraft(this.data.types);
    const tabBar = this.getTabBar && this.getTabBar();
    if (tabBar) tabBar.setSelected(2);
    this.setTabBarVisible(!this.data.editing);
  },
  setTabBarVisible(visible) {
    const tabBar = this.getTabBar && this.getTabBar();
    if (tabBar) {
      tabBar.setData({ hidden: !visible });
      return;
    }
    if (visible) wx.showTabBar({ animation: true });
    else wx.hideTabBar({ animation: true });
  },
  applyEditDraft(types) {
    const draft = wx.getStorageSync("activityEditDraft");
    if (!draft || draft.id === this.editId) return;
    this.editId = draft.id;
    wx.removeStorageSync("activityEditDraft");
    const typeIndex = types.findIndex((type) => type.name === draft.gameType);
    const start = String(draft.startTime || "").split("T");
    const form = { title: draft.title || "", gameTypeId: typeIndex >= 0 ? types[typeIndex].id : null, coverUrl: draft.coverUrl || "", date: start[0] || tomorrow(), time: (start[1] || "19:30").slice(0, 5), maxPeople: String(draft.maxPeople || 6), city: draft.city || "", storeName: draft.storeName || "", address: draft.address || "", longitude: draft.longitude || null, latitude: draft.latitude || null, fee: String(draft.fee || 0), aa: draft.aa !== false, newbieFriendly: draft.newbieFriendly !== false, durationMinutes: String(draft.durationMinutes || 120), difficulty: draft.difficulty || "轻松", language: draft.language || "中文", teachingProvided: draft.teachingProvided !== false, bringGame: draft.bringGame !== false, description: draft.description || "" };
    this.setData({ editing: true, form, typeIndex, hasType: typeIndex >= 0, typeLabel: typeIndex >= 0 ? types[typeIndex].name : "请选择游戏类型", coverTheme: coverTheme(draft.gameType), cityLabel: draft.city || "请选择城市" }, () => {
      this.editSnapshot = this.formSnapshot();
      this.setTabBarVisible(false);
    });
  },
  formSnapshot() {
    const form = this.data.form;
    return JSON.stringify({ ...form, gameTypeId: form.gameTypeId || null });
  },
  hasUnsavedChanges() { return this.data.editing && this.editSnapshot && this.editSnapshot !== this.formSnapshot(); },
  backToDetail() {
    if (this.data.submitting) return;
    if (this.hasUnsavedChanges()) {
      wx.showModal({
        title: "修改尚未保存",
        content: "是否保存本次修改？",
        confirmText: "保存修改",
        cancelText: "不保存",
        success: (result) => {
          if (result.confirm) this.submit();
          else this.discardAndReturn();
        }
      });
      return;
    }
    this.discardAndReturn();
  },
  discardAndReturn() {
    const id = this.editId;
    this.editId = null;
    this.editSnapshot = null;
    this.setData({ editing: false });
    this.setTabBarVisible(true);
    if (id) wx.redirectTo({ url: `/pages/detail/index?id=${id}` });
    else wx.switchTab({ url: "/pages/create/index" });
  },
  changeField(e) { this.setData({ [`form.${e.currentTarget.dataset.field}`]: e.detail.value }); },
  changeDifficulty(e) { this.setData({ "form.difficulty": ["轻松", "进阶", "硬核"][Number(e.detail.value)] }); },
  changeType(e) {
    const typeIndex = Number(e.detail.value);
    const type = this.data.types[typeIndex];
    this.setData({ typeIndex, hasType: true, typeLabel: type.name, coverTheme: coverTheme(type.name), "form.gameTypeId": type.id });
  },
  chooseCover() {
    if (this.data.uploadingCover) return;
    wx.chooseMedia({ count: 1, mediaType: ["image"], sizeType: ["compressed"], success: ({ tempFiles }) => {
      const file = tempFiles && tempFiles[0];
      if (!file) return;
      this.setData({ uploadingCover: true });
      getApp().ensureSession().then(() => api.uploadActivityCover(file.tempFilePath)).then((coverUrl) => this.setData({ "form.coverUrl": coverUrl })).catch((error) => wx.showToast({ title: error.message, icon: "none" })).finally(() => this.setData({ uploadingCover: false }));
    } });
  },
  chooseLocation() {
    if (!wx.chooseLocation) { wx.showToast({ title: "当前微信版本不支持地图选点", icon: "none" }); return; }
    const selectLocation = () => wx.chooseLocation({
      success: (location) => {
        const city = cityFromAddress(location.address);
        if (!city) { wx.showToast({ title: "未识别到城市，请重新选择地点", icon: "none" }); return; }
        this.setData({
          cityLabel: city,
          "form.city": city,
          "form.storeName": location.name || "",
          "form.address": location.address || "",
          "form.longitude": location.longitude,
          "form.latitude": location.latitude
        });
      },
      fail: (error) => {
        if (!/cancel/.test(error.errMsg || "")) this.showLocationError(error);
      }
    });
    wx.getSetting({
      success: ({ authSetting }) => {
        if (authSetting["scope.userLocation"] === false) { this.openLocationSettings(); return; }
        if (authSetting["scope.userLocation"]) { selectLocation(); return; }
        wx.authorize({ scope: "scope.userLocation", success: selectLocation, fail: () => this.openLocationSettings() });
      },
      fail: selectLocation
    });
  },
  openLocationSettings() {
    wx.showModal({ title: "需要位置权限", content: "开启位置权限后，才能搜索地点或在地图上选点。", confirmText: "去设置", success: (result) => { if (result.confirm) wx.openSetting({}); } });
  },
  showLocationError(error) {
    const message = (error && error.errMsg) || "未知错误";
    wx.showModal({ title: "地图选点失败", content: `请确认已开启位置权限后重试。\n${message}`, showCancel: false });
  },
  changeSwitch(e) { this.setData({ [`form.${e.currentTarget.dataset.field}`]: e.detail.value }); },
  submit() {
    const form = this.data.form;
    const hasGameType = form.gameTypeId;
    const required = ["title", "date", "time", "city", "address", "description"];
    if (!hasGameType || required.some((key) => !form[key])) { wx.showToast({ title: "请补全必填信息", icon: "none" }); return; }
    this.setData({ submitting: true });
    const payload = { ...form, maxPeople: Number(form.maxPeople), fee: Number(form.fee), durationMinutes: Number(form.durationMinutes), startTime: `${form.date}T${form.time}:00` };
    const publish = () => this.data.editing ? api.updateActivity(this.editId, payload) : api.createActivity(payload);
    getApp().ensureSession()
      .catch(() => { throw new Error("登录后才能发布活动"); })
      .then(publish)
      .catch((error) => {
        if (!/用户不存在/.test(error.message || "")) throw error;
        getApp().clearSession();
        return getApp().ensureSession().then(publish);
      })
      .then((activity) => { const message = this.data.editing ? "修改成功" : "发布成功"; this.editId = null; this.editSnapshot = null; this.setData({ editing: false }); this.setTabBarVisible(true); wx.showToast({ title: message, icon: "success" }); setTimeout(() => wx.redirectTo({ url: `/pages/detail/index?id=${activity.id}` }), 500); })
      .catch((e) => wx.showToast({ title: e.message, icon: "none" }))
      .finally(() => this.setData({ submitting: false }));
  }
});
