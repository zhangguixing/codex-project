package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.entity.Message;
import com.zhuoyouquan.mapper.MessageMapper;
import com.zhuoyouquan.vo.MessageVO;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.servlet.http.HttpServletRequest;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/messages")
public class MessageController {
    private final MessageMapper messages;

    public MessageController(MessageMapper messages) {
        this.messages = messages;
    }

    @GetMapping
    @Operation(summary = "我的消息")
    public Result<List<MessageVO>> list(HttpServletRequest request) {
        Long userId = (Long) request.getAttribute(ApiConstants.USER_ID);
        List<MessageVO> records = messages.selectList(new LambdaQueryWrapper<Message>()
                        .eq(Message::getUserId, userId)
                        .orderByDesc(Message::getCreatedAt))
                .stream().map(this::toVO).toList();
        return Result.ok(records);
    }

    private MessageVO toVO(Message message) {
        MessageVO value = new MessageVO();
        value.setId(message.getId());
        value.setType(message.getType());
        value.setTitle(message.getTitle());
        value.setContent(message.getContent());
        value.setTargetId(message.getTargetId());
        value.setCreatedAt(message.getCreatedAt());
        return value;
    }
}
