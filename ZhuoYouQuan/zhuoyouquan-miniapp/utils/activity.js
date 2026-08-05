function formatActivityTime(value) {
  const raw = String(value || "");
  const matched = raw.match(/^(\d{4})-(\d{1,2})-(\d{1,2})[T\s](\d{1,2}):(\d{2})/);
  if (!matched) return raw.replace("T", " ") || "时间待定";

  const [, year, month, day, hour, minute] = matched;
  const date = new Date(Number(year), Number(month) - 1, Number(day));
  const now = new Date();
  const weekStart = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 6) % 7));
  const nextWeekStart = new Date(weekStart);
  nextWeekStart.setDate(weekStart.getDate() + 7);
  const weekAfterNextStart = new Date(nextWeekStart);
  weekAfterNextStart.setDate(nextWeekStart.getDate() + 7);
  const weekdays = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
  const base = `${Number(month)}月${Number(day)}日 ${String(hour).padStart(2, "0")}:${minute}`;

  if (date >= weekStart && date < nextWeekStart) return `${base} ${weekdays[date.getDay()]}`;
  if (date >= nextWeekStart && date < weekAfterNextStart) return `${base} 下${weekdays[date.getDay()]}`;
  return `${year}年${Number(month)}月${Number(day)}日 ${String(hour).padStart(2, "0")}:${minute}`;
}

function decorateActivity(activity) {
  return { ...activity, displayTime: formatActivityTime(activity && activity.startTime) };
}

function decorateActivityPage(page) {
  return { ...page, records: (page.records || []).map(decorateActivity) };
}

module.exports = { decorateActivity, decorateActivityPage, formatActivityTime };
