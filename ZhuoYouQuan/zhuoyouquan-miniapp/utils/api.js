const { request } = require("./request");
const { mockLogin } = require("./config");
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

module.exports = {
  login,
  isLoggedIn,
  gameTypes: () => request({ url: "/api/game-types" }),
  activities: (data) => request({ url: "/api/activities", data }).then(decorateActivityPage),
  activity: (id) => request({ url: `/api/activities/${id}` }).then(decorateActivity),
  createActivity: (data) => request({ url: "/api/activities", method: "POST", data }),
  updateActivity: (id, data) => request({ url: `/api/activities/${id}`, method: "PUT", data }),
  joinActivity: (id) => request({ url: `/api/activities/${id}/join`, method: "POST" }),
  profile: () => request({ url: "/api/users/me" }),
  myActivities: (role) => request({ url: "/api/activities/mine", data: { role } }).then(decorateActivityPage)
};
