/**
 * EMS AI Chatbot — Google Gemini Style Floating Widget
 * Gọi API /api/ai/chat (proxy qua MVC AiChatController)
 */
(function () {
    'use strict';

    // ============ CONFIG ============
    const CHAT_API_URL = '/AiChat/Chat';

    // ============ STATE ============
    let isOpen = false;
    let isLoading = false;
    const chatHistory = []; // { role: "user" | "model", content: string }

    // ============ DOM REFERENCES ============
    let fab, chatWindow, messagesContainer, inputField, sendBtn;

    // SVG Gemini Spark Icon (4-point star with gradient fill)
    const GEMINI_SPARK_SVG = `
    <svg class="gemini-spark-icon" viewBox="0 0 24 24" width="22" height="22" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 0C12 6.62742 6.62742 12 0 12C6.62742 12 12 17.3726 12 24C12 17.3726 17.3726 12 24 12C17.3726 12 12 6.62742 12 0Z" fill="url(#geminiSparkGrad)"/>
        <defs>
            <linearGradient id="geminiSparkGrad" x1="0" y1="0" x2="24" y2="24" gradientUnits="userSpaceOnUse">
                <stop offset="0%" stop-color="#4285F4"/>
                <stop offset="50%" stop-color="#9B51E0"/>
                <stop offset="100%" stop-color="#D9657B"/>
            </linearGradient>
        </defs>
    </svg>`;

    // ============ INITIALIZATION ============
    document.addEventListener('DOMContentLoaded', function () {
        createChatWidget();

        // Nạp lại lịch sử nếu có, ngược lại hiện tin chào mừng
        const loaded = loadHistoryFromSession();
        if (!loaded) {
            addWelcomeMessage();
        }
    });

    function createChatWidget() {
        // Floating Action Button (FAB)
        fab = document.createElement('button');
        fab.className = 'ai-chat-fab';
        fab.setAttribute('aria-label', 'Mở AI Chat');
        fab.setAttribute('title', 'Trợ lý AI — Tư vấn sự kiện');
        fab.innerHTML = GEMINI_SPARK_SVG;
        fab.addEventListener('click', toggleChat);
        document.body.appendChild(fab);

        // Chat Window
        chatWindow = document.createElement('div');
        chatWindow.className = 'ai-chat-window';
        chatWindow.innerHTML = `
            <div class="ai-chat-header">
                <div class="ai-chat-header-left">
                    <div class="ai-chat-header-avatar">
                        ${GEMINI_SPARK_SVG}
                    </div>
                    <div class="ai-chat-header-info">
                        <h4>EMS AI</h4>
                        <p>Trợ lý tư vấn sự kiện thông minh</p>
                    </div>
                </div>
                <button class="ai-chat-reset-btn" id="aiChatResetBtn" title="Bắt đầu cuộc hội thoại mới">
                    <i class="ri-refresh-line"></i> Bắt đầu lại
                </button>
            </div>
            <div class="ai-chat-messages" id="aiChatMessages"></div>
            <div class="ai-chat-input-area">
                <div class="ai-chat-input-wrapper">
                    <input type="text" id="aiChatInput" placeholder="Hỏi AI về sự kiện, đăng ký, check-in..." autocomplete="off" />
                    <button class="ai-chat-send-btn" id="aiChatSendBtn" title="Gửi">
                        <i class="ri-arrow-up-line"></i>
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(chatWindow);

        // Get references
        messagesContainer = document.getElementById('aiChatMessages');
        inputField = document.getElementById('aiChatInput');
        sendBtn = document.getElementById('aiChatSendBtn');

        // Events
        sendBtn.addEventListener('click', sendMessage);
        inputField.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        inputField.addEventListener('input', function () {
            if (inputField.value.trim().length > 0) {
                sendBtn.classList.add('has-text');
            } else {
                sendBtn.classList.remove('has-text');
            }
        });

        // Event nút reset
        const resetBtn = document.getElementById('aiChatResetBtn');
        resetBtn.addEventListener('click', resetChat);

        // Event nút "Hiển thị sự kiện"
        messagesContainer.addEventListener('click', function (e) {
            const btn = e.target.closest('.ai-show-card-btn');
            if (btn) {
                const title = btn.getAttribute('data-event-title') || '';
                const promptMsg = `Hiển thị thẻ sự kiện ${title}`;
                inputField.value = promptMsg;
                sendMessage();
            }
        });
    }

    function toggleChat() {
        isOpen = !isOpen;
        if (isOpen) {
            chatWindow.classList.add('open');
            fab.classList.add('active');
            fab.innerHTML = '<i class="ri-close-line"></i>';
            setTimeout(() => inputField.focus(), 350);
        } else {
            chatWindow.classList.remove('open');
            fab.classList.remove('active');
            fab.innerHTML = GEMINI_SPARK_SVG;
        }
    }

    // ============ EVENT BUTTON RENDERER ============
    function renderEventButtons(html) {
        if (!html) return html;

        const btnRegex = /\[BTN:\s*id=([^|\]]+)\s*\|\s*title=([^\]]+)\]/gi;

        return html.replace(btnRegex, function (match, id, title) {
            id = (id || '').trim();
            title = (title || '').trim();

            return `<button type="button" class="ai-show-card-btn" data-event-id="${id}" data-event-title="${title}"><i class="ri-layout-grid-line"></i> Hiển thị sự kiện</button>`;
        });
    }

    // ============ EVENT CARD RENDERER ============
    function renderEventCards(html) {
        if (!html) return html;

        // Clean up <pre><code> wrappers if marked turned [CARD: ...] into code blocks
        html = html.replace(/<pre><code[^>]*>([\s\S]*?)<\/code><\/pre>/gi, function (match, codeContent) {
            if (codeContent.includes('[CARD:')) {
                return codeContent
                    .replace(/&lt;/g, '<')
                    .replace(/&gt;/g, '>')
                    .replace(/&amp;/g, '&');
            }
            return match;
        });

        // Clean up <p> tags wrapping [CARD: ...]
        html = html.replace(/<p>\s*(\[CARD:[\s\S]*?\])\s*<\/p>/gi, '$1');

        // Convert Vietnamese hallucinated tag blocks to standard card format
        const vnCardBlockRegex = /\[TÊN_SỰ_KIỆN:\s*([^\]]+)\]\s*\[ID_SỰ_KIỆN:\s*([^\]]+)\](?:\s*\[HÌNH_ẢNH:\s*([^\]]*)\])?\s*\[THỜI_GIAN:\s*([^\]]+)\]\s*\[ĐỊA_ĐIỂM:\s*([^\]]+)\]\s*\[SỨC_CHỨA:\s*([^\]]+)\]\s*\[GIÁ_VÉ:\s*([^\]]+)\](?:[\s\S]*?)(?=\n\n|$|\[|<)/gi;
        html = html.replace(vnCardBlockRegex, function(m, title, id, image, time, loc, cap, price) {
            return `[CARD: id=${id.trim()} | title=${title.trim()} | image=${(image||'').trim()} | time=${time.trim()} | location=${loc.trim()} | capacity=${cap.trim()} | price=${price.trim()}]`;
        });

        const cardRegex = /\[CARD:\s*id=([^|\]]+)\s*\|\s*title=([^|\]]+)(?:\s*\|\s*image=([^|\]]*))?\s*\|\s*time=([^|\]]+)\s*\|\s*location=([^|\]]+)\s*\|\s*capacity=([^|\]]+)\s*\|\s*price=([^|\]]+)(?:[\s\S]*?)\]/gi;

        return html.replace(cardRegex, function (match, id, title, image, time, location, capacity, price) {
            id = (id || '').trim();
            title = (title || '').trim();
            image = (image || '').replace(/<[^>]*>/g, '').trim(); // Clean any HTML tags
            time = (time || '').trim();
            location = (location || '').trim();
            capacity = (capacity || '').trim();
            price = (price || '').trim();

            const defaultBg = 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)';
            let bannerContent = '';

            if (image && image.length > 5 && !image.toLowerCase().includes('null')) {
                bannerContent = `<img src="${image}" alt="${title}" class="ai-card-img" onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';" /><div class="ai-card-banner-fallback" style="display:none; background: ${defaultBg};"><i class="ri-calendar-event-fill" style="font-size: 2.2rem; color: #a8c7fa; opacity: 0.85;"></i></div>`;
            } else {
                bannerContent = `<div class="ai-card-banner-fallback" style="display:flex; background: ${defaultBg};"><i class="ri-calendar-event-fill" style="font-size: 2.2rem; color: #a8c7fa; opacity: 0.85;"></i></div>`;
            }

            return `<div class="ai-event-card"><div class="ai-card-banner">${bannerContent}<span class="ai-card-badge">${price}</span></div><div class="ai-card-content"><h5 class="ai-card-title">${title}</h5><div class="ai-card-info"><div class="ai-card-info-item"><i class="ri-time-line"></i> <span>${time}</span></div><div class="ai-card-info-item"><i class="ri-map-pin-line"></i> <span>${location}</span></div><div class="ai-card-info-item"><i class="ri-user-line"></i> <span>Sức chứa: ${capacity} chỗ</span></div></div><a href="/Events/Detail/${id}" class="ai-card-btn"><i class="ri-ticket-2-line"></i> Xem chi tiết & Đăng ký</a></div></div>`;
        });
    }

    // ============ MARKDOWN PARSER ============
    function parseMarkdown(text) {
        if (!text) return '';
        let parsedHtml = text;

        if (window.marked && typeof window.marked.parse === 'function') {
            try {
                parsedHtml = window.marked.parse(text);
            } catch (e) {
                console.error('Marked parsing error:', e);
            }
        } else {
            // Custom lightweight Markdown parser fallback
            let html = text
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
                .replace(/\*(.*?)\*/g, '<em>$1</em>')
                .replace(/`([^`]+)`/g, '<code>$1</code>')
                .replace(/^\s*[\-\*]\s+(.*)$/gm, '<li>$1</li>');

            html = html.replace(/(<li>[\s\S]*?<\/li>)+/g, function (match) {
                return '<ul>' + match + '</ul>';
            });

            parsedHtml = html.split('\n\n').map(p => {
                if (p.startsWith('<ul>') || p.startsWith('<ol>') || p.includes('class="ai-event-card"')) return p;
                return '<p>' + p.replace(/\n/g, '<br/>') + '</p>';
            }).join('');
        }

        parsedHtml = renderEventButtons(parsedHtml);
        return renderEventCards(parsedHtml);
    }

    // ============ STATE PERSISTENCE ============

    function saveHistoryToSession() {
        sessionStorage.setItem('ems_chat_history', JSON.stringify(chatHistory));
    }

    function loadHistoryFromSession() {
        const saved = sessionStorage.getItem('ems_chat_history');
        if (saved) {
            try {
                const parsed = JSON.parse(saved);
                parsed.forEach(msg => {
                    renderMessageRow(msg.role, msg.content);
                    chatHistory.push(msg);
                });
                return parsed.length > 0;
            } catch (e) {
                sessionStorage.removeItem('ems_chat_history');
            }
        }
        return false;
    }

    function resetChat() {
        if (confirm('Bạn có chắc chắn muốn xóa lịch sử trò chuyện và bắt đầu cuộc hội thoại mới không?')) {
            sessionStorage.removeItem('ems_chat_history');
            chatHistory.length = 0;
            messagesContainer.innerHTML = '';
            addWelcomeMessage();
        }
    }

    // ============ MESSAGES ============

    function addWelcomeMessage() {
        const welcomeText = '👋 **Xin chào! Mình là Trợ lý AI của hệ thống EMS.**\n\n' +
            'Mình có thể giúp bạn:\n' +
            '- 🔍 **Tìm kiếm sự kiện** phù hợp\n' +
            '- 📝 **Hướng dẫn đăng ký** tham gia\n' +
            '- ✅ **Hướng dẫn check-in** tích lũy điểm rèn luyện\n\n' +
            'Hãy hỏi mình bất cứ điều gì nhé! 😊';
        appendBotMessage(welcomeText);
    }

    function renderMessageRow(role, content) {
        const row = document.createElement('div');
        row.className = `ai-chat-row ${role === 'user' ? 'user' : 'bot'}`;

        if (role === 'user') {
            const bubble = document.createElement('div');
            bubble.className = 'ai-chat-user-bubble';
            bubble.textContent = content;
            row.appendChild(bubble);
        } else {
            const avatar = document.createElement('div');
            avatar.className = 'ai-chat-bot-avatar';
            avatar.innerHTML = GEMINI_SPARK_SVG;

            const textContentContainer = document.createElement('div');
            textContentContainer.className = 'ai-chat-bot-text';
            textContentContainer.innerHTML = parseMarkdown(content);

            row.appendChild(avatar);
            row.appendChild(textContentContainer);
        }

        messagesContainer.appendChild(row);
        scrollToBottom();
    }

    function appendBotMessage(text) {
        renderMessageRow('model', text);
        chatHistory.push({ role: 'model', content: text });
        saveHistoryToSession();
    }

    function appendUserMessage(text) {
        renderMessageRow('user', text);
        chatHistory.push({ role: 'user', content: text });
        saveHistoryToSession();
    }

    function showTypingIndicator() {
        const row = document.createElement('div');
        row.className = 'ai-chat-row bot';
        row.id = 'aiChatTyping';

        const avatar = document.createElement('div');
        avatar.className = 'ai-chat-bot-avatar';
        avatar.innerHTML = GEMINI_SPARK_SVG;

        const typing = document.createElement('div');
        typing.className = 'ai-chat-typing';
        typing.innerHTML = '<span></span><span></span><span></span>';

        row.appendChild(avatar);
        row.appendChild(typing);
        messagesContainer.appendChild(row);
        scrollToBottom();
    }

    function removeTypingIndicator() {
        const typing = document.getElementById('aiChatTyping');
        if (typing) typing.remove();
    }

    function scrollToBottom() {
        setTimeout(() => {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }, 50);
    }

    // ============ SEND MESSAGE ============

    function sanitizeBotReply(userMessage, reply) {
        if (!reply) return reply;

        const agreeKeywords = ['có', 'co', 'ok', 'đăng ký', 'dang ky', 'muốn', 'muon', 'cho xin thẻ', 'cho xin the', 'xem thẻ', 'xem the', 'lấy vé', 'lay ve', 'vâng', 'vang', 'đồng ý', 'dong y', 'thẻ', 'the'];
        const msgLower = (userMessage || '').toLowerCase().trim();

        // Check if user message expresses intent to register or see cards
        const userAgreed = agreeKeywords.some(kw => msgLower.includes(kw));

        if (!userAgreed) {
            // Strip [CARD: ...] tags from reply if user hasn't agreed yet
            return reply.replace(/\[CARD:[\s\S]*?\]/gi, '').trim();
        }

        return reply;
    }

    async function sendMessage() {
        const text = inputField.value.trim();
        if (!text || isLoading) return;

        appendUserMessage(text);
        inputField.value = '';
        sendBtn.classList.remove('has-text');
        isLoading = true;
        sendBtn.disabled = true;
        inputField.disabled = true;

        showTypingIndicator();

        try {
            // Gửi lịch sử ngoại trừ tin nhắn mới nhất vừa được push
            const pastHistory = chatHistory.slice(0, -1);

            const response = await fetch(CHAT_API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message: text,
                    history: pastHistory
                })
            });

            removeTypingIndicator();

            if (response.ok) {
                const data = await response.json();
                const cleanReply = sanitizeBotReply(text, data.reply || 'Xin lỗi, mình chưa hiểu câu hỏi này. 😅');
                appendBotMessage(cleanReply);
            } else {
                appendBotMessage('⚠️ Đã xảy ra lỗi khi kết nối. Vui lòng thử lại nhé!');
            }
        } catch (err) {
            removeTypingIndicator();
            appendBotMessage('⚠️ Không thể kết nối. Kiểm tra kết nối mạng và thử lại nhé!');
        } finally {
            isLoading = false;
            sendBtn.disabled = false;
            inputField.disabled = false;
            inputField.focus();
        }
    }
})();
