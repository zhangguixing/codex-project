package com.zhuoyouquan.service;

import com.zhuoyouquan.dto.UserProfileRequest;
import com.zhuoyouquan.vo.UserVO;

public interface UserService {
    UserVO getProfile(Long userId);

    UserVO updateProfile(Long userId, UserProfileRequest request);
}
