package com.zhuoyouquan.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.plugins.pagination.Page;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.dto.ActivityCreateRequest;
import com.zhuoyouquan.dto.ActivityQuery;
import com.zhuoyouquan.entity.Activity;
import com.zhuoyouquan.entity.ActivityJoin;
import com.zhuoyouquan.entity.Favorite;
import com.zhuoyouquan.entity.GameType;
import com.zhuoyouquan.mapper.ActivityJoinMapper;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.mapper.FavoriteMapper;
import com.zhuoyouquan.mapper.GameTypeMapper;
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
    private final UserServiceImpl users;

    public ActivityServiceImpl(ActivityMapper activities, ActivityJoinMapper joins, GameTypeMapper games, FavoriteMapper favorites, UserServiceImpl users) {
        this.activities = activities;
        this.joins = joins;
        this.games = games;
        this.favorites = favorites;
        this.users = users;
    }

    @Override
    public PageVO<ActivityVO> list(ActivityQuery q, Long viewerId) {
        int page = Math.max(1, q.getPage() == null ? 1 : q.getPage()), size = Math.min(50, Math.max(1, q.getSize() == null ? 10 : q.getSize()));
        LambdaQueryWrapper<Activity> w = new LambdaQueryWrapper<Activity>().eq(Activity::getStatus, "OPEN");
        if (q.getCity() != null && !q.getCity().isBlank()) w.eq(Activity::getCity, q.getCity());
        if (q.getGameTypeId() != null) w.eq(Activity::getGameTypeId, q.getGameTypeId());
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
        v.setParticipants(joins.selectList(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, id).eq(ActivityJoin::getStatus, "JOINED")).stream().map(j -> users.getProfile(j.getUserId())).toList());
        return v;
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
        a.setUpdatedAt(LocalDateTime.now());
        activities.updateById(a);
        return toVO(a, userId, false);
    }

    private GameType resolveGameType(ActivityCreateRequest r) {
        if (r.getGameTypeId() != null) return games.selectById(r.getGameTypeId());
        String name = r.getCustomGameType() == null ? "" : r.getCustomGameType().trim();
        if (name.isEmpty()) throw new BizException("请选择或填写游戏类型");
        GameType existing = games.selectOne(new LambdaQueryWrapper<GameType>().eq(GameType::getName, name));
        if (existing != null) return existing;
        GameType custom = new GameType();
        custom.setName(name);
        custom.setEnabled(1);
        custom.setSortOrder(999);
        games.insert(custom);
        return custom;
    }
        @Override @Transactional public void join (Long userId, Long activityId){
            Activity a = activities.selectForUpdate(activityId);
            if (a == null) throw new BizException("活动不存在");
            if (!"OPEN".equals(a.getStatus()) || a.getStartTime().isBefore(LocalDateTime.now()))
                throw new BizException("该活动暂不可报名");
            if (a.getOrganizerId().equals(userId))
                throw new BizException("不能报名自己发起的活动");ActivityJoin existed=joins.selectOne(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId,activityId).eq(ActivityJoin::getUserId,userId));if(existed!=null&&"JOINED".equals(existed.getStatus()))throw new BizException("您已报名该活动");
            if (a.getJoinedPeople() >= a.getMaxPeople()) throw new BizException("活动名额已满");
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
        @Override @Transactional public void cancelJoin (Long userId, Long activityId){
            Activity a = activities.selectForUpdate(activityId);
            if (a == null)
                throw new BizException("活动不存在");ActivityJoin join=joins.selectOne(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId,activityId).eq(ActivityJoin::getUserId,userId).eq(ActivityJoin::getStatus,"JOINED"));if(join==null)throw new BizException("没有可取消的报名");if(a.getStartTime().isBefore(LocalDateTime.now()))throw new BizException("活动已开始，无法取消报名");join.setStatus("CANCELED");join.setCanceledAt(LocalDateTime.now());joins.updateById(join);a.setJoinedPeople(Math.max(0,a.getJoinedPeople()-1));if("FULL".equals(a.getStatus()))a.setStatus("OPEN");a.setUpdatedAt(LocalDateTime.now());activities.updateById(a); }
            public PageVO<ActivityVO> mine (Long userId, String role){
                List<Long> ids = joins.selectList(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getUserId, userId).eq(ActivityJoin::getStatus, "JOINED")).stream().map(ActivityJoin::getActivityId).toList();
                List<Activity> records = "created".equals(role) ? activities.selectList(new LambdaQueryWrapper<Activity>().eq(Activity::getOrganizerId, userId).orderByDesc(Activity::getCreatedAt)) : ids.isEmpty() ? List.of() : activities.selectBatchIds(ids);
                return new PageVO<>(records.size(), 1, records.size(), records.stream().map(a -> toVO(a, userId, false)).toList());
            }
            private void applyTime (LambdaQueryWrapper < Activity > w, String range){
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
            private Activity requireActivity (Long id){
                Activity a = activities.selectById(id);
                if (a == null) throw new BizException("活动不存在");
                return a;
            }
            private ActivityVO toVO (Activity a, Long viewerId,boolean includeOrganizer){
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
                v.setStatus(a.getStatus());
                v.setCreatedAt(a.getCreatedAt());
                if (includeOrganizer) v.setOrganizer(users.getProfile(a.getOrganizerId()));
                if (viewerId != null) {
                    v.setJoined(joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, a.getId()).eq(ActivityJoin::getUserId, viewerId).eq(ActivityJoin::getStatus, "JOINED")) > 0);
                    v.setFavorited(favorites.selectCount(new LambdaQueryWrapper<Favorite>().eq(Favorite::getUserId, viewerId).eq(Favorite::getTargetType, "ACTIVITY").eq(Favorite::getTargetId, a.getId())) > 0);
                }
                return v;
            }
        }
