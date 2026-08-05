package com.zhuoyouquan.service;

import com.zhuoyouquan.vo.AuthVO;

public interface AuthService {
    AuthVO login(String code);
}
