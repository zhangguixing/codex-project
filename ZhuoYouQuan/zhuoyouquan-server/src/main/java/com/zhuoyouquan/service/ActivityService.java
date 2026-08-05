package com.zhuoyouquan.service;

import com.zhuoyouquan.dto.ActivityCreateRequest;
import com.zhuoyouquan.dto.ActivityQuery;
import com.zhuoyouquan.vo.ActivityVO;
import com.zhuoyouquan.vo.PageVO;

public interface ActivityService {
    PageVO<ActivityVO> list(ActivityQuery query, Long viewerId);

    ActivityVO detail(Long id, Long viewerId);

    ActivityVO create(Long userId, ActivityCreateRequest request);

    ActivityVO update(Long userId, Long activityId, ActivityCreateRequest request);

    void join(Long userId, Long activityId);

    void cancelJoin(Long userId, Long activityId);
}
