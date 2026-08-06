package com.zhuoyouquan.mapper;

import com.baomidou.mybatisplus.core.mapper.BaseMapper;
import com.zhuoyouquan.entity.Activity;
import org.apache.ibatis.annotations.Mapper;
import org.apache.ibatis.annotations.Select;

@Mapper
public interface ActivityMapper extends BaseMapper<Activity> {
    @Select("SELECT * FROM activity WHERE id = #{id} AND deleted = 0 FOR UPDATE")
    Activity selectForUpdate(Long id);
}
