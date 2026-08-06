const { baseUrl } = require("./config");

function request(options) {
  const token = wx.getStorageSync("token");
  const data = options.data && Object.fromEntries(
    Object.entries(options.data).filter(([, value]) => value !== undefined && value !== null && value !== "")
  );
  return new Promise((resolve, reject) => {
    wx.request({
      ...options,
      url: baseUrl + options.url,
      data,
      header: {
        "content-type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(options.header || {})
      },
      success(response) {
        const body = response && response.data;
        if (response.statusCode >= 200 && response.statusCode < 300 && body && body.code === 0) {
          resolve(body.data);
          return;
        }
        const message = (body && body.message) || `请求失败 (${response.statusCode || "未知状态"})`;
        reject(new Error(message));
      },
      fail(error) {
        reject(new Error((error && error.errMsg) || "网络连接失败，请检查本地服务"));
      }
    });
  });
}

module.exports = { request };
