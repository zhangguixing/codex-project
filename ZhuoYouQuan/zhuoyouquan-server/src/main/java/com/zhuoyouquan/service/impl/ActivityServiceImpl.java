package com.zhuoyouquan.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.dto.ActivityCreateRequest;
import com.zhuoyouquan.dto.ActivityMessageRequest;
import com.zhuoyouquan.dto.ActivityQuery;
import com.zhuoyouquan.entity.*;
import com.zhuoyouquan.mapper.*;
import com.zhuoyouquan.service.ActivityService;
import com.zhuoyouquan.vo.ActivityVO;
import com.zhuoyouquan.vo.PageVO;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Comparator;
import java.util.List;

@Service
public class ActivityServiceImpl implements ActivityService {
    private final ActivityMapper activities;
    private final ActivityJoinMapper joins;
    private final GameTypeMapper games;
    private final FavoriteMapper favorites;
    private final MessageMapper messages;
    private final UserServiceImpl users;

    public ActivityServiceImpl(ActivityMapper activities, ActivityJoinMapper joins, GameTypeMapper games, FavoriteMapper favorites, MessageMapper messages, UserServiceImpl users) {
        this.activities = activities;
        this.joins = joins;
        this.games = games;
        this.favorites = favorites;
        this.messages = messages;
        this.users = users;
    }

    @Override
    public PageVO<ActivityVO> list(ActivityQuery q, Long viewerId) {
        int page = Math.max(1, q.getPage() == null ? 1 : q.getPage()), size = Math.min(50, Math.max(1, q.getSize() == null ? 10 : q.getSize()));
        LambdaQueryWrapper<Activity> w = new LambdaQueryWrapper<Activity>()
                .in(Activity::getStatus, "OPEN", "FULL")
                .gt(Activity::getStartTime, LocalDateTime.now());
        if (q.getCity() != null && !q.getCity().isBlank()) w.eq(Activity::getCity, q.getCity());
        if (q.getKeyword() != null && !q.getKeyword().isBlank()) w.like(Activity::getTitle, q.getKeyword().trim());
        if (q.getGameTypeId() != null) w.eq(Activity::getGameTypeId, q.getGameTypeId());
        if (q.getNewbieFriendly() != null) w.eq(Activity::getNewbieFriendly, q.getNewbieFriendly());
        if (q.getDifficulty() != null && !q.getDifficulty().isBlank()) w.eq(Activity::getDifficulty, q.getDifficulty());
        if (q.getMaxFee() != null) w.le(Activity::getFee, q.getMaxFee());
        if (q.getMinSlots() != null) w.apply("max_people - joined_people >= {0}", q.getMinSlots());
        applyTime(w, q.getTimeRange());
        if (q.getLatitude() != null && q.getLongitude() != null) {
            List<Activity> records = activities.selectList(w);
            records.sort(Comparator.comparingDouble(a -> distance(a, q.getLatitude(), q.getLongitude())));
            int from = Math.min((page - 1) * size, records.size());
            int to = Math.min(from + size, records.size());
            return new PageVO<>(records.size(), page, size, records.subList(from, to).stream().map(x -> toVO(x, viewerId, false)).toList());
        }
        if ("popular".equals(q.getSort())) w.orderByDesc(Activity::getJoinedPeople);
        else if ("soon".equals(q.getSort())) w.orderByAsc(Activity::getStartTime);
        else w.orderByDesc(Activity::getCreatedAt);
        Page<Activity> result = activities.selectPage(new Page<>(page, size), w);
        return new PageVO<>(result.getTotal(), result.getCurrent(), result.getSize(), result.getRecords().stream().map(x -> toVO(x, viewerId, false)).toList());
    }

    @Override
    @Transactional
    public void leaveMessage(Long userId, Long activityId, ActivityMessageRequest request) {
        Activity activity = requireActivity(activityId);
        if (activity.getOrganizerId().equals(userId)) throw new BizException("不能给自己留言");

        String content = request.getContent().trim();
        if (content.isEmpty()) throw new BizException("留言内容不能为空");
        String senderName = users.requireUser(userId).getNickname();

        Message message = new Message();
        message.setUserId(activity.getOrganizerId());
        message.setType("ACTIVITY_MESSAGE");
        message.setTitle(senderName + " 留言了你的活动");
        message.setContent(content);
        message.setTargetId(activityId);
        message.setCreatedAt(LocalDateTime.now());
        messages.insert(message);
    }

    @Override
    @Transactional
    public void end(Long userId, Long activityId) {
        Activity activity = activities.selectForUpdate(activityId);
        if (activity == null) throw new BizException("活动不存在");
        if (!activity.getOrganizerId().equals(userId)) throw new BizException("只有发起人可以结束活动");
        if (!"OPEN".equals(activity.getStatus()) && !"FULL".equals(activity.getStatus()))
            throw new BizException("该活动已结束");

        activity.setStatus("ENDED");
        activity.setUpdatedAt(LocalDateTime.now());
        activities.updateById(activity);

        List<ActivityJoin> participants = joins.selectList(new LambdaQueryWrapper<ActivityJoin>()
                .eq(ActivityJoin::getActivityId, activityId)
                .eq(ActivityJoin::getStatus, "JOINED"));
        LocalDateTime now = LocalDateTime.now();
        for (ActivityJoin participant : participants) {
            Message message = new Message();
            message.setUserId(participant.getUserId());
            message.setType("ACTIVITY_ENDED");
            message.setTitle("活动已结束");
            message.setContent("你报名的《" + activity.getTitle() + "》已被发起人结束。");
            message.setTargetId(activityId);
            message.setCreatedAt(now);
            messages.insert(message);
        }
    }

    private double distance(Activity activity, double latitude, double longitude) {
        if (activity.getLatitude() == null || activity.getLongitude() == null) return Double.MAX_VALUE;
        double earthRadiusKm = 6371d;
        double lat1 = Math.toRadians(latitude), lat2 = Math.toRadians(activity.getLatitude().doubleValue());
        double deltaLat = lat2 - lat1;
        double deltaLon = Math.toRadians(activity.getLongitude().doubleValue() - longitude);
        double haversine = Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2)
                + Math.cos(lat1) * Math.cos(lat2) * Math.sin(deltaLon / 2) * Math.sin(deltaLon / 2);
        return 2 * earthRadiusKm * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
    }

    @Override
    public ActivityVO detail(Long id, Long viewerId) {
        Activity a = requireActivity(id);
        ActivityVO v = toVO(a, viewerId, true);
        List<ActivityJoin> participants = joins.selectList(new LambdaQueryWrapper<ActivityJoin>()
                .eq(ActivityJoin::getActivityId, id).eq(ActivityJoin::getStatus, "JOINED"));
        v.setParticipants(participants.stream().map(j -> users.getProfile(j.getUserId())).toList());
        v.setCheckedInParticipantIds(participants.stream().filter(j -> j.getCheckedInAt() != null).map(ActivityJoin::getUserId).toList());
        return v;
    }

    @Override
    @Transactional
    public void checkIn(Long organizerId, Long activityId, Long participantId) {
        Activity activity = activities.selectForUpdate(activityId);
        if (activity == null) throw new BizException("活动不存在");
        if (!activity.getOrganizerId().equals(organizerId)) throw new BizException("只有发起人可以签到");
        if (activity.getStartTime().isAfter(LocalDateTime.now().plusHours(2)))
            throw new BizException("活动开始前两小时才可签到");
        ActivityJoin join = joins.selectOne(new LambdaQueryWrapper<ActivityJoin>()
                .eq(ActivityJoin::getActivityId, activityId)
                .eq(ActivityJoin::getUserId, participantId)
                .eq(ActivityJoin::getStatus, "JOINED"));
        if (join == null) throw new BizException("该用户不在报名名单中");
        if (join.getCheckedInAt() == null) {
            join.setCheckedInAt(LocalDateTime.now());
            joins.updateById(join);
        }
    }

    @Override
    @Transactional
    public void broadcast(Long organizerId, Long activityId, ActivityMessageRequest request) {
        Activity activity = requireActivity(activityId);
        if (!activity.getOrganizerId().equals(organizerId)) throw new BizException("只有发起人可以发送通知");
        String content = request.getContent().trim();
        if (content.isEmpty()) throw new BizException("通知内容不能为空");
        LocalDateTime now = LocalDateTime.now();
        for (ActivityJoin join : joins.selectList(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, activityId).eq(ActivityJoin::getStatus, "JOINED"))) {
            Message message = new Message();
            message.setUserId(join.getUserId());
            message.setType("ACTIVITY_BROADCAST");
            message.setTitle("发起人通知");
            message.setContent("《" + activity.getTitle() + "》：" + content);
            message.setTargetId(activityId);
            message.setCreatedAt(now);
            messages.insert(message);
        }
    }

    @Override
    @Transactional
    public ActivityVO create(Long userId, ActivityCreateRequest r) {
        users.requireUser(userId);
        GameType game = resolveGameType(r);
        if (game == null || !Integer.valueOf(1).equals(game.getEnabled()))
            throw new BizException("桌游类型不可用");
        Activity a = new Activity();
        a.setOrganizerId(userId);
        a.setGameTypeId(game.getId());
        a.setTitle(r.getTitle());
        a.setCoverUrl(r.getCoverUrl());
        a.setStartTime(r.getStartTime());
        a.setMaxPeople(r.getMaxPeople());
        a.setJoinedPeople(0);
        a.setCity(r.getCity());
        a.setStoreName(r.getStoreName());
        a.setAddress(r.getAddress());
        a.setLongitude(r.getLongitude());
        a.setLatitude(r.getLatitude());
        a.setFee(r.getFee());
        a.setAa(r.getAa());
        a.setDescription(r.getDescription());
        a.setNewbieFriendly(r.getNewbieFriendly());
        a.setDurationMinutes(r.getDurationMinutes());
        a.setDifficulty(r.getDifficulty());
        a.setLanguage(r.getLanguage());
        a.setTeachingProvided(r.getTeachingProvided());
        a.setBringGame(r.getBringGame());
        a.setStatus("OPEN");
        a.setCreatedAt(LocalDateTime.now());
        a.setUpdatedAt(LocalDateTime.now());
        activities.insert(a);
        return toVO(a, userId, false);
    }

    @Override
    @Transactional
    public ActivityVO update(Long userId, Long activityId, ActivityCreateRequest r) {
        Activity a = requireActivity(activityId);
        if (!a.getOrganizerId().equals(userId)) throw new BizException("只有发起人可以修改活动");
        if (!"OPEN".equals(a.getStatus()) || a.getStartTime().isBefore(LocalDateTime.now()))
            throw new BizException("当前活动不可修改");
        GameType game = resolveGameType(r);
        if (game == null || !Integer.valueOf(1).equals(game.getEnabled())) throw new BizException("桌游类型不可用");
        a.setGameTypeId(game.getId());
        a.setTitle(r.getTitle());
        a.setCoverUrl(r.getCoverUrl());
        a.setStartTime(r.getStartTime());
        a.setMaxPeople(r.getMaxPeople());
        a.setCity(r.getCity());
        a.setStoreName(r.getStoreName());
        a.setAddress(r.getAddress());
        a.setLongitude(r.getLongitude());
        a.setLatitude(r.getLatitude());
        a.setFee(r.getFee());
        a.setAa(r.getAa());
        a.setDescription(r.getDescription());
        a.setNewbieFriendly(r.getNewbieFriendly());
        a.setDurationMinutes(r.getDurationMinutes());
        a.setDifficulty(r.getDifficulty());
        a.setLanguage(r.getLanguage());
        a.setTeachingProvided(r.getTeachingProvided());
        a.setBringGame(r.getBringGame());
        a.setUpdatedAt(LocalDateTime.now());
        activities.updateById(a);
        return toVO(a, userId, false);
    }

    private GameType resolveGameType(ActivityCreateRequest r) {
        if (r.getGameTypeId() != null) return games.selectById(r.getGameTypeId());
        throw new BizException("请选择游戏类型");
    }

    @Override
    @Transactional
    public void join(Long userId, Long activityId) {
        Activity a = activities.selectForUpdate(activityId);
        if (a == null) throw new BizException("活动不存在");
        if ((!"OPEN".equals(a.getStatus()) && !"FULL".equals(a.getStatus())) || a.getStartTime().isBefore(LocalDateTime.now()))
            throw new BizException("该活动暂不可报名");
        if (a.getOrganizerId().equals(userId))
            throw new BizException("不能报名自己发起的活动");
        ActivityJoin existed = joins.selectOne(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, activityId).eq(ActivityJoin::getUserId, userId));
        if (existed != null && "JOINED".equals(existed.getStatus())) throw new BizException("您已报名该活动");
        if (a.getJoinedPeople() >= a.getMaxPeople()) {
            if (existed == null) {
                existed = new ActivityJoin();
                existed.setActivityId(activityId);
                existed.setUserId(userId);
                existed.setCreatedAt(LocalDateTime.now());
                joins.insert(existed);
            }
            existed.setStatus("WAITLISTED");
            existed.setCanceledAt(null);
            joins.updateById(existed);
            return;
        }
        if (existed == null) {
            existed = new ActivityJoin();
            existed.setActivityId(activityId);
            existed.setUserId(userId);
            existed.setCreatedAt(LocalDateTime.now());
            joins.insert(existed);
        }
        existed.setStatus("JOINED");
        existed.setCanceledAt(null);
        joins.updateById(existed);
        a.setJoinedPeople(a.getJoinedPeople() + 1);
        if (a.getJoinedPeople() >= a.getMaxPeople()) a.setStatus("FULL");
        a.setUpdatedAt(LocalDateTime.now());
        activities.updateById(a);
    }

    @Override
    @Transactional
    public void cancelJoin(Long userId, Long activityId) {
        Activity a = activities.selectForUpdate(activityId);
        if (a == null)
            throw new BizException("活动不存在");
        ActivityJoin join = joins.selectOne(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, activityId).eq(ActivityJoin::getUserId, userId).in(ActivityJoin::getStatus, "JOINED", "WAITLISTED"));
        if (join == null) throw new BizException("没有可取消的报名");
        if (a.getStartTime().isBefore(LocalDateTime.now())) throw new BizException("活动已开始，无法取消报名");
        boolean wasJoined = "JOINED".equals(join.getStatus());
        join.setStatus("CANCELED");
        join.setCanceledAt(LocalDateTime.now());
        joins.updateById(join);
        if (wasJoined) {
            a.setJoinedPeople(Math.max(0, a.getJoinedPeople() - 1));
            ActivityJoin next = joins.selectOne(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, activityId).eq(ActivityJoin::getStatus, "WAITLISTED").orderByAsc(ActivityJoin::getCreatedAt).last("LIMIT 1"));
            if (next != null) {
                next.setStatus("JOINED");
                joins.updateById(next);
                a.setJoinedPeople(a.getJoinedPeople() + 1);
                Message message = new Message();
                message.setUserId(next.getUserId());
                message.setType("WAITLIST_PROMOTED");
                message.setTitle("候补转正");
                message.setContent("你已递补进入《" + a.getTitle() + "》");
                message.setTargetId(activityId);
                message.setCreatedAt(LocalDateTime.now());
                messages.insert(message);
            }
        }
        a.setStatus(a.getJoinedPeople() >= a.getMaxPeople() ? "FULL" : "OPEN");
        a.setUpdatedAt(LocalDateTime.now());
        activities.updateById(a);
    }

    public PageVO<ActivityVO> mine(Long userId, String role) {
        List<Long> ids = joins.selectList(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getUserId, userId).eq(ActivityJoin::getStatus, "JOINED")).stream().map(ActivityJoin::getActivityId).toList();
        List<Activity> records = "created".equals(role) ? activities.selectList(new LambdaQueryWrapper<Activity>().eq(Activity::getOrganizerId, userId).orderByDesc(Activity::getCreatedAt)) : ids.isEmpty() ? List.of() : activities.selectBatchIds(ids);
        return new PageVO<>(records.size(), 1, records.size(), records.stream().map(a -> toVO(a, userId, false)).toList());
    }

    private void applyTime(LambdaQueryWrapper<Activity> w, String range) {
        if (range == null) return;
        LocalDate today = LocalDate.now();
        if ("today".equals(range))
            w.between(Activity::getStartTime, today.atStartOfDay(), today.plusDays(1).atStartOfDay());
        else if ("tomorrow".equals(range))
            w.between(Activity::getStartTime, today.plusDays(1).atStartOfDay(), today.plusDays(2).atStartOfDay());
        else if ("weekend".equals(range)) {
            LocalDate saturday = today.plusDays((6 - today.getDayOfWeek().getValue() + 7) % 7);
            w.between(Activity::getStartTime, saturday.atStartOfDay(), saturday.plusDays(2).atStartOfDay());
        }
    }

    private Activity requireActivity(Long id) {
        Activity a = activities.selectById(id);
        if (a == null) throw new BizException("活动不存在");
        return a;
    }

    private ActivityVO toVO(Activity a, Long viewerId, boolean includeOrganizer) {
        ActivityVO v = new ActivityVO();
        v.setId(a.getId());
        v.setTitle(a.getTitle());
        v.setCoverUrl(a.getCoverUrl());
        GameType g = games.selectById(a.getGameTypeId());
        v.setGameType(g == null ? "未知" : g.getName());
        v.setStartTime(a.getStartTime());
        v.setMaxPeople(a.getMaxPeople());
        v.setJoinedPeople(a.getJoinedPeople());
        v.setCity(a.getCity());
        v.setStoreName(a.getStoreName());
        v.setAddress(a.getAddress());
        v.setLongitude(a.getLongitude());
        v.setLatitude(a.getLatitude());
        v.setFee(a.getFee());
        v.setAa(a.getAa());
        v.setDescription(a.getDescription());
        v.setNewbieFriendly(a.getNewbieFriendly());
        v.setDurationMinutes(a.getDurationMinutes());
        v.setDifficulty(a.getDifficulty());
        v.setLanguage(a.getLanguage());
        v.setTeachingProvided(a.getTeachingProvided());
        v.setBringGame(a.getBringGame());
        v.setStatus(effectiveStatus(a));
        v.setCreatedAt(a.getCreatedAt());
        if (includeOrganizer) v.setOrganizer(users.getProfile(a.getOrganizerId()));
        if (viewerId != null) {
            v.setJoined(joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, a.getId()).eq(ActivityJoin::getUserId, viewerId).eq(ActivityJoin::getStatus, "JOINED")) > 0);
            v.setWaitlisted(joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, a.getId()).eq(ActivityJoin::getUserId, viewerId).eq(ActivityJoin::getStatus, "WAITLISTED")) > 0);
            v.setFavorited(favorites.selectCount(new LambdaQueryWrapper<Favorite>().eq(Favorite::getUserId, viewerId).eq(Favorite::getTargetType, "ACTIVITY").eq(Favorite::getTargetId, a.getId())) > 0);
        }
        v.setWaitlistCount(joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, a.getId()).eq(ActivityJoin::getStatus, "WAITLISTED")).intValue());
        return v;
    }

    private String effectiveStatus(Activity activity) {
        if (("OPEN".equals(activity.getStatus()) || "FULL".equals(activity.getStatus()))
                && !activity.getStartTime().isAfter(LocalDateTime.now())) return "ENDED";
        return activity.getStatus();
    }
}
