const { request } = require("./request");
const { mockLogin, baseUrl } = require("./config");
const { decorateActivity, decorateActivityPage } = require("./activity");

function login() {
  const requestProfile = new Promise((resolve, reject) => {
    if (!wx.getUserProfile) { resolve(null); return; }
    wx.getUserProfile({
      desc: "用于创建桌游玩家档案",
      success: resolve,
      fail: () => reject(new Error("已取消微信授权，请授权后继续"))
    });
  });
  const getCode = mockLogin
    ? Promise.resolve("local-miniapp-user")
    : new Promise((resolve, reject) => wx.login({ success: (res) => res.code ? resolve(res.code) : reject(new Error("未获取到微信登录凭证")), fail: (err) => reject(new Error((err && err.errMsg) || "微信登录失败")) }));
  return Promise.all([getCode, requestProfile])
    .then(([code, profile]) => request({ url: "/api/auth/wechat-login", method: "POST", data: { code } }).then((session) => ({ session, profile })))
    .then(({ session, profile }) => {
      wx.setStorageSync("token", session.token);
      const userInfo = profile && profile.userInfo;
      if (!userInfo) return session.user;
      return request({ url: "/api/users/me", method: "PUT", data: { nickname: userInfo.nickName, avatar: userInfo.avatarUrl } });
    });
}

function isLoggedIn() {
  return Boolean(wx.getStorageSync("token"));
}

function uploadActivityCover(filePath) {
  const token = wx.getStorageSync("token");
  return new Promise((resolve, reject) => wx.uploadFile({
    url: `${baseUrl}/api/uploads/activity-cover`, filePath, name: "file",
    header: token ? { Authorization: `Bearer ${token}` } : {},
    success: (response) => { try { const body = JSON.parse(response.data); if (response.statusCode >= 200 && response.statusCode < 300 && body.code === 0) resolve(body.data.url); else reject(new Error(body.message || "封面上传失败")); } catch { reject(new Error("封面上传失败")); } },
    fail: (error) => reject(new Error((error && error.errMsg) || "封面上传失败"))
  }));
}

module.exports = {
  login,
  isLoggedIn,
  uploadActivityCover,
  gameTypes: () => request({ url: "/api/game-types" }),
  activities: (data) => request({ url: "/api/activities", data }).then(decorateActivityPage),
  activity: (id) => request({ url: `/api/activities/${id}` }).then(decorateActivity),
  createActivity: (data) => request({ url: "/api/activities", method: "POST", data }),
  updateActivity: (id, data) => request({ url: `/api/activities/${id}`, method: "PUT", data }),
  joinActivity: (id) => request({ url: `/api/activities/${id}/join`, method: "POST" }),
  cancelJoinActivity: (id) => request({ url: `/api/activities/${id}/join`, method: "DELETE" }),
  endActivity: (id) => request({ url: `/api/activities/${id}/end`, method: "POST" }),
  leaveActivityMessage: (id, content) => request({ url: `/api/activities/${id}/messages`, method: "POST", data: { content } }),
  activityChat: (id) => request({ url: `/api/activities/${id}/chat` }),
  sendActivityChat: (id, content, parentId) => request({ url: `/api/activities/${id}/chat`, method: "POST", data: { content, parentId } }),
  checkInParticipant: (id, participantId) => request({ url: `/api/activities/${id}/participants/${participantId}/check-in`, method: "POST" }),
  broadcastActivity: (id, content) => request({ url: `/api/activities/${id}/broadcasts`, method: "POST", data: { content } }),
  reviewActivity: (id, data) => request({ url: `/api/activities/${id}/reviews`, method: "POST", data }),
  report: (data) => request({ url: "/api/reports", method: "POST", data }),
  blockUser: (id) => request({ url: `/api/users/${id}/block`, method: "POST" }),
  stores: (data) => request({ url: "/api/stores", data }),
  store: (id) => request({ url: `/api/stores/${id}` }),
  activityShareCode: (id) => request({ url: `/api/activities/${id}/share-code` }),
  profile: () => request({ url: "/api/users/me" }),
  messages: () => request({ url: "/api/messages" }),
  myActivities: (role) => request({ url: "/api/activities/mine", data: { role } }).then(decorateActivityPage)
};
