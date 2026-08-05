package com.zhuoyouquan.dto;

import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Future;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import lombok.Data;

@Data
public class ActivityCreateRequest {
    @NotBlank(message = "活动标题不能为空")
    @Size(max = 80)
    private String title;

    private Long gameTypeId;

    @Size(max = 32, message = "自定义游戏类型不能超过32个字符")
    private String customGameType;

    private String coverUrl;

    @NotNull(message = "请选择活动时间")
    @Future(message = "活动时间必须晚于当前时间")
    private LocalDateTime startTime;

    @NotNull
    @Min(value = 2, message = "至少需要2名玩家")
    @Max(value = 100, message = "人数不能超过100")
    private Integer maxPeople;

    @NotBlank(message = "请选择城市")
    private String city;

    private String storeName;

    @NotBlank(message = "请填写详细地址")
    private String address;

    private BigDecimal longitude;
    private BigDecimal latitude;

    @NotNull
    @DecimalMin(value = "0.0", message = "费用不能为负数")
    private BigDecimal fee;

    @NotNull
    private Boolean aa;

    @NotBlank(message = "请填写活动说明")
    @Size(max = 2000)
    private String description;

    @NotNull
    private Boolean newbieFriendly;
}
