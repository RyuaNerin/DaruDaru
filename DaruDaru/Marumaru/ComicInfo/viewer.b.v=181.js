"use strict";
function _classCallCheck(e, t) {
    if (!(e instanceof t))
        throw new TypeError("Cannot call a class as a function")
}
function getDocHeight() {
    return Math.max(document.body.scrollHeight, document.documentElement.scrollHeight, document.body.offsetHeight, document.documentElement.offsetHeight, document.body.clientHeight, document.documentElement.clientHeight)
}
function update_viewed_data() {
    try {
        set_cookie("last_wr_id", chapter, 1),
        set_cookie("last_page", cur_page, 1),
        set_cookie("last_percent", localStorage.getItem("scroll:" + chapter), 1)
    } catch (e) {}
}
function on_fullscreen() {
    var e = $(".viewer-con")[0];
    (e.requestFullscreen || e.webkitRequestFullScreen || e.mozRequestFullScreen || e.msRequestFullscreen).call(e)
}
function on_viewer_left() {
    wait_paging || (0 != cur_page || 1 != one_page && 2 != one_page) && (1 == one_page ? open_page(cur_page - 1) : 0 == one_page ? open_page(cur_page) : 2 == one_page && open_page(cur_page - 1))
}
function page_selected() {
    wait_paging && (wait_paging.onload = null,
    wait_paging = null),
    open_page($("#page-selector").val())
}
function on_viewer_right() {
    if (!wait_paging)
        return cur_page >= img_list.length - 1 && (0 == one_page || 2 == one_page) ? on_close_viewer() : void (1 == one_page ? open_page(cur_page) : 0 == one_page ? open_page(cur_page + 1) : 2 == one_page && open_page(cur_page + 1))
}
function on_toggle_onepage_mode(e) {
    one_page_mode = !one_page_mode,
    open_page(cur_page),
    one_page_mode ? ($(e).removeClass("fa-pause"),
    $(e).addClass("fa-square"),
    $(e).find("span").text("한쪽")) : ($(e).addClass("fa-pause"),
    $(e).removeClass("fa-square"),
    $(e).find("span").text("기본"))
}
function on_close_viewer() {
    $(".manga-viewer-modal").css("opacity", 0),
    setTimeout(function() {
        $(".manga-viewer-modal").css("display", "none"),
        $("html, body").css("overflow", "scroll")
    }, 500)
}
function open_page(e) {
    try {
        cur_page = Number(e);
        var t = img_list1[cur_page] || img_list[cur_page];
        update_viewed_data(),
        0 == cur_page ? $(".page-btn").css("opacity", 1) : $(".page-btn").css("opacity", 0);
        var n = $("#canvas-viewer")[0]
          , a = n.getContext("2d");
        n.oncontextmenu = function(e) {
            return e.preventDefault(),
            !1
        }
        ,
        n.width = 1e3,
        n.height = 1e3,
        a.clearRect(0, 0, n.width, n.height),
        wait_paging = new Image,
        wait_paging.src = t,
        wait_paging.onerror = function() {
            t != img_list[cur_page] ? t = img_list[cur_page] : -1 == t.indexOf("s3") && (t = t.replace("img.", "s3.")),
            wait_paging.src = t
        }
        ,
        wait_paging.onload = function() {
            console.log(">>", wait_paging.naturalWidth, wait_paging.naturalHeight);
            var e = wait_paging.naturalWidth
              , t = wait_paging.naturalHeight;
            e > t && 1 == one_page_mode ? (n.width = e / 2,
            n.height = t,
            1 != one_page ? (vv1.rtc(n, null, wait_paging, 1),
            one_page = 1) : (vv1.rtc(n, null, wait_paging, 2),
            one_page = 0)) : (n.width = e,
            n.height = t,
            vv1.rtc(n, null, wait_paging, 0),
            one_page = 2),
            wait_paging = null,
            $(window).resize(),
            $("#page-selector").unbind("onchange"),
            $("#page-selector").val(cur_page),
            $("#page-selector").bind("onchange", page_selected)
        }
    } catch (e) {
        alert(e)
    }
}
function on_open_viewer() {
    var e = $(".manga-viewer-modal");
    e.css("display", "flex"),
    e.css("opacity", 1),
    is_first && (open_page(cur_page),
    is_first = !1),
    $("html, body").css("overflow", "hidden"),
    setTimeout(function() {
        $("html, body").css("overflow", "hidden")
    }, 10)
}
function certify_win_open(e, t, n) {
    if (void 0 === n && (n = window.event),
    "kcb-ipin" == e) {
        var a = window.open(t, "kcbPop", "left=200, top=100, status=0, width=450, height=550");
        a.focus()
    } else if ("kcb-hp" == e) {
        var a = window.open(t, "auth_popup", "left=200, top=100, width=430, height=590, scrollbar=yes");
        a.focus()
    } else if ("kcp-hp" == e)
        if ($("input[name=veri_up_hash]").size() < 1 && $("input[name=cert_no]").after('<input type="hidden" name="veri_up_hash" value="">'),
        navigator.userAgent.indexOf("Android") > -1 || navigator.userAgent.indexOf("iPhone") > -1) {
            var r = $(n.target.form);
            $("#kcp_cert").size() < 1 ? (r.wrap('<div id="cert_info"></div>'),
            $("#cert_info").append('<form name="form_temp" method="post">')) : $("#kcp_cert").remove(),
            $("#cert_info").after('<iframe id="kcp_cert" name="kcp_cert" width="100%" height="700" frameborder="0" scrolling="no" style="display:none"></iframe>');
            var i = document.form_temp;
            i.target = "kcp_cert",
            i.action = t,
            document.getElementById("cert_info").style.display = "none",
            document.getElementById("kcp_cert").style.display = "",
            i.submit()
        } else {
            var o = 410
              , c = 500
              , s = screen.width / 2 - o / 2
              , l = screen.height / 2 - c / 2
              , d = "width=" + o + ", height=" + c + ", toolbar=no,status=no,statusbar=no,menubar=no,scrollbars=no,resizable=no"
              , m = ",left=" + s + ", top=" + l;
            window.open(t, "auth_popup", d + m)
        }
    else if ("lg-hp" == e)
        if (g5_is_mobile) {
            var r = $(n.target.form);
            $("#lgu_cert").size() < 1 ? (r.wrap('<div id="cert_info"></div>'),
            $("#cert_info").append('<form name="form_temp" method="post">')) : $("#lgu_cert").remove(),
            $("#cert_info").after('<iframe id="lgu_cert" name="lgu_cert" width="100%" src="' + t + '" height="700" frameborder="0" scrolling="no" style="display:none"></iframe>'),
            document.getElementById("cert_info").style.display = "none",
            document.getElementById("lgu_cert").style.display = ""
        } else {
            var o = 640
              , c = 660
              , s = screen.width / 2 - o / 2
              , l = screen.height / 2 - c / 2
              , a = window.open(t, "auth_popup", "left=" + s + ", top=" + l + ", width=" + o + ", height=" + c + ", scrollbar=yes");
            a.focus()
        }
}
function cert_confirm() {
    var e;
    switch (document.fregisterform.cert_type.value) {
    case "ipin":
        e = "아이핀";
        break;
    case "hp":
        e = "휴대폰";
        break;
    default:
        return !0
    }
    return !!confirm("이미 " + e + "으로 본인확인을 완료하셨습니다.\n\n이전 인증을 취소하고 다시 인증하시겠습니까?")
}
var _createClass = function() {
    function e(e, t) {
        for (var n = 0; n < t.length; n++) {
            var a = t[n];
            a.enumerable = a.enumerable || !1,
            a.configurable = !0,
            "value"in a && (a.writable = !0),
            Object.defineProperty(e, a.key, a)
        }
    }
    return function(t, n, a) {
        return n && e(t.prototype, n),
        a && e(t, a),
        t
    }
}();
if (function(e, t, n) {
    var a, r = e.getElementsByTagName(t)[0];
    e.getElementById(n) || (a = e.createElement(t),
    a.id = n,
    a.src = "//connect.facebook.net/ko_KR/all.js#xfbml=1",
    r.parentNode.insertBefore(a, r))
}(document, "script", "facebook-jssdk"),
function(e, t, n) {
    var a, r = e.getElementsByTagName(t)[0];
    e.getElementById(n) || (a = e.createElement(t),
    a.id = n,
    a.src = "https://platform.twitter.com/widgets.js",
    r.parentNode.insertBefore(a, r))
}(document, "script", "twitter-wjs"),
window.___gcfg = {
    lang: "ko",
    parsetags: "onload"
},
function() {
    var e = document.createElement("script");
    e.type = "text/javascript",
    e.async = !0,
    e.src = "https://apis.google.com/js/plusone.js";
    var t = document.getElementsByTagName("script")[0];
    t.parentNode.insertBefore(e, t)
}(),
void 0 === MD5_JS)
    var hex_md5 = function(e) {
        return binl2hex(core_md5(str2binl(e), e.length * chrsz))
    }
      , b64_md5 = function(e) {
        return binl2b64(core_md5(str2binl(e), e.length * chrsz))
    }
      , str_md5 = function(e) {
        return binl2str(core_md5(str2binl(e), e.length * chrsz))
    }
      , hex_hmac_md5 = function(e, t) {
        return binl2hex(core_hmac_md5(e, t))
    }
      , b64_hmac_md5 = function(e, t) {
        return binl2b64(core_hmac_md5(e, t))
    }
      , str_hmac_md5 = function(e, t) {
        return binl2str(core_hmac_md5(e, t))
    }
      , core_md5 = function(e, t) {
        e[t >> 5] |= 128 << t % 32,
        e[14 + (t + 64 >>> 9 << 4)] = t;
        for (var n = 1732584193, a = -271733879, r = -1732584194, i = 271733878, o = 0; o < e.length; o += 16) {
            var c = n
              , s = a
              , l = r
              , d = i;
            n = md5_ff(n, a, r, i, e[o + 0], 7, -680876936),
            i = md5_ff(i, n, a, r, e[o + 1], 12, -389564586),
            r = md5_ff(r, i, n, a, e[o + 2], 17, 606105819),
            a = md5_ff(a, r, i, n, e[o + 3], 22, -1044525330),
            n = md5_ff(n, a, r, i, e[o + 4], 7, -176418897),
            i = md5_ff(i, n, a, r, e[o + 5], 12, 1200080426),
            r = md5_ff(r, i, n, a, e[o + 6], 17, -1473231341),
            a = md5_ff(a, r, i, n, e[o + 7], 22, -45705983),
            n = md5_ff(n, a, r, i, e[o + 8], 7, 1770035416),
            i = md5_ff(i, n, a, r, e[o + 9], 12, -1958414417),
            r = md5_ff(r, i, n, a, e[o + 10], 17, -42063),
            a = md5_ff(a, r, i, n, e[o + 11], 22, -1990404162),
            n = md5_ff(n, a, r, i, e[o + 12], 7, 1804603682),
            i = md5_ff(i, n, a, r, e[o + 13], 12, -40341101),
            r = md5_ff(r, i, n, a, e[o + 14], 17, -1502002290),
            a = md5_ff(a, r, i, n, e[o + 15], 22, 1236535329),
            n = md5_gg(n, a, r, i, e[o + 1], 5, -165796510),
            i = md5_gg(i, n, a, r, e[o + 6], 9, -1069501632),
            r = md5_gg(r, i, n, a, e[o + 11], 14, 643717713),
            a = md5_gg(a, r, i, n, e[o + 0], 20, -373897302),
            n = md5_gg(n, a, r, i, e[o + 5], 5, -701558691),
            i = md5_gg(i, n, a, r, e[o + 10], 9, 38016083),
            r = md5_gg(r, i, n, a, e[o + 15], 14, -660478335),
            a = md5_gg(a, r, i, n, e[o + 4], 20, -405537848),
            n = md5_gg(n, a, r, i, e[o + 9], 5, 568446438),
            i = md5_gg(i, n, a, r, e[o + 14], 9, -1019803690),
            r = md5_gg(r, i, n, a, e[o + 3], 14, -187363961),
            a = md5_gg(a, r, i, n, e[o + 8], 20, 1163531501),
            n = md5_gg(n, a, r, i, e[o + 13], 5, -1444681467),
            i = md5_gg(i, n, a, r, e[o + 2], 9, -51403784),
            r = md5_gg(r, i, n, a, e[o + 7], 14, 1735328473),
            a = md5_gg(a, r, i, n, e[o + 12], 20, -1926607734),
            n = md5_hh(n, a, r, i, e[o + 5], 4, -378558),
            i = md5_hh(i, n, a, r, e[o + 8], 11, -2022574463),
            r = md5_hh(r, i, n, a, e[o + 11], 16, 1839030562),
            a = md5_hh(a, r, i, n, e[o + 14], 23, -35309556),
            n = md5_hh(n, a, r, i, e[o + 1], 4, -1530992060),
            i = md5_hh(i, n, a, r, e[o + 4], 11, 1272893353),
            r = md5_hh(r, i, n, a, e[o + 7], 16, -155497632),
            a = md5_hh(a, r, i, n, e[o + 10], 23, -1094730640),
            n = md5_hh(n, a, r, i, e[o + 13], 4, 681279174),
            i = md5_hh(i, n, a, r, e[o + 0], 11, -358537222),
            r = md5_hh(r, i, n, a, e[o + 3], 16, -722521979),
            a = md5_hh(a, r, i, n, e[o + 6], 23, 76029189),
            n = md5_hh(n, a, r, i, e[o + 9], 4, -640364487),
            i = md5_hh(i, n, a, r, e[o + 12], 11, -421815835),
            r = md5_hh(r, i, n, a, e[o + 15], 16, 530742520),
            a = md5_hh(a, r, i, n, e[o + 2], 23, -995338651),
            n = md5_ii(n, a, r, i, e[o + 0], 6, -198630844),
            i = md5_ii(i, n, a, r, e[o + 7], 10, 1126891415),
            r = md5_ii(r, i, n, a, e[o + 14], 15, -1416354905),
            a = md5_ii(a, r, i, n, e[o + 5], 21, -57434055),
            n = md5_ii(n, a, r, i, e[o + 12], 6, 1700485571),
            i = md5_ii(i, n, a, r, e[o + 3], 10, -1894986606),
            r = md5_ii(r, i, n, a, e[o + 10], 15, -1051523),
            a = md5_ii(a, r, i, n, e[o + 1], 21, -2054922799),
            n = md5_ii(n, a, r, i, e[o + 8], 6, 1873313359),
            i = md5_ii(i, n, a, r, e[o + 15], 10, -30611744),
            r = md5_ii(r, i, n, a, e[o + 6], 15, -1560198380),
            a = md5_ii(a, r, i, n, e[o + 13], 21, 1309151649),
            n = md5_ii(n, a, r, i, e[o + 4], 6, -145523070),
            i = md5_ii(i, n, a, r, e[o + 11], 10, -1120210379),
            r = md5_ii(r, i, n, a, e[o + 2], 15, 718787259),
            a = md5_ii(a, r, i, n, e[o + 9], 21, -343485551),
            n = safe_add(n, c),
            a = safe_add(a, s),
            r = safe_add(r, l),
            i = safe_add(i, d)
        }
        return Array(n, a, r, i)
    }
      , md5_cmn = function(e, t, n, a, r, i) {
        return safe_add(bit_rol(safe_add(safe_add(t, e), safe_add(a, i)), r), n)
    }
      , md5_ff = function(e, t, n, a, r, i, o) {
        return md5_cmn(t & n | ~t & a, e, t, r, i, o)
    }
      , md5_gg = function(e, t, n, a, r, i, o) {
        return md5_cmn(t & a | n & ~a, e, t, r, i, o)
    }
      , md5_hh = function(e, t, n, a, r, i, o) {
        return md5_cmn(t ^ n ^ a, e, t, r, i, o)
    }
      , md5_ii = function(e, t, n, a, r, i, o) {
        return md5_cmn(n ^ (t | ~a), e, t, r, i, o)
    }
      , core_hmac_md5 = function(e, t) {
        var n = str2binl(e);
        n.length > 16 && (n = core_md5(n, e.length * chrsz));
        for (var a = Array(16), r = Array(16), i = 0; i < 16; i++)
            a[i] = 909522486 ^ n[i],
            r[i] = 1549556828 ^ n[i];
        var o = core_md5(a.concat(str2binl(t)), 512 + t.length * chrsz);
        return core_md5(r.concat(o), 640)
    }
      , safe_add = function(e, t) {
        var n = (65535 & e) + (65535 & t);
        return (e >> 16) + (t >> 16) + (n >> 16) << 16 | 65535 & n
    }
      , bit_rol = function(e, t) {
        return e << t | e >>> 32 - t
    }
      , str2binl = function(e) {
        for (var t = Array(), n = (1 << chrsz) - 1, a = 0; a < e.length * chrsz; a += chrsz)
            t[a >> 5] |= (e.charCodeAt(a / chrsz) & n) << a % 32;
        return t
    }
      , binl2str = function(e) {
        for (var t = "", n = (1 << chrsz) - 1, a = 0; a < 32 * e.length; a += chrsz)
            t += String.fromCharCode(e[a >> 5] >>> a % 32 & n);
        return t
    }
      , binl2hex = function(e) {
        for (var t = hexcase ? "0123456789ABCDEF" : "0123456789abcdef", n = "", a = 0; a < 4 * e.length; a++)
            n += t.charAt(e[a >> 2] >> a % 4 * 8 + 4 & 15) + t.charAt(e[a >> 2] >> a % 4 * 8 & 15);
        return n
    }
      , binl2b64 = function(e) {
        for (var t = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/", n = "", a = 0; a < 4 * e.length; a += 3)
            for (var r = (e[a >> 2] >> a % 4 * 8 & 255) << 16 | (e[a + 1 >> 2] >> (a + 1) % 4 * 8 & 255) << 8 | e[a + 2 >> 2] >> (a + 2) % 4 * 8 & 255, i = 0; i < 4; i++)
                8 * a + 6 * i > 32 * e.length ? n += b64pad : n += t.charAt(r >> 6 * (3 - i) & 63);
        return n
    }
      , MD5_JS = !0
      , hexcase = 0
      , b64pad = ""
      , chrsz = 8;
!function(e) {
    var t, n = /a/i, a = {
        liitem: "li",
        item: "a",
        next: "[&gt;{5}]",
        prev: "[{4}&lt;]",
        format: "[{0}]",
        itemClass: "",
        appendhtml: "",
        sideClass: "paging-side",
        prevClass: "paging-side",
        itemCurrent: "active",
        length: 10,
        max: 1,
        current: 1,
        append: !1,
        href: "#{0}",
        event: !0,
        first: "[1&lt;&lt;]",
        last: "[&gt;&gt;{6}]"
    }, r = function(e, t) {
        return e.indexOf(t)
    }, i = function(e) {
        var t = arguments;
        return e.replace(/\{(\d+)\}/g, function(e, n) {
            return +n < 0 ? e : t[+n + 1] || ""
        })
    }, o = function(a, o, c, s) {
        var l = !1;
        if (r(c, a.itemCurrent) > -1 ? (t = document.createElement("strong"),
        l = !0) : t = document.createElement(a.item),
        t.className = c,
        t.innerHTML = i(s, o, a.length, a.start, a.end, a.start - 1, a.end + 1, a.max),
        n.test(a.item) && (t.href = i(a.href, o)),
        a.event) {
            e(t).bind("click", function(n) {
                var r = !0;
                return e.isFunction(a.onclick) && (r = a.onclick.call(t, n, o, a)),
                (void 0 == r || r) && a.origin.paging(e.extend({}, a, {
                    current: o
                })),
                r
            }),
            a.appendhtml && e(t).append(a.appendhtml),
            e(t).appendTo(a.origin),
            l ? e(t).prepend('<span class="sound_only">열린</span>') : a.origin.append("\n");
            var d = "on";
            switch (s) {
            case a.prev:
                d += "prev";
                break;
            case a.next:
                d += "next";
                break;
            case a.first:
                d += "first";
                break;
            case a.last:
                d += "last";
                break;
            default:
                d += "item"
            }
            e.isFunction(a[d]) && a[d].call(t, o, a)
        }
        return t
    };
    e.fn.paging = function(t) {
        t = e.extend({
            origin: this
        }, a, t || {}),
        this.html(""),
        t.max < 1 && (t.max = 1),
        t.current < 1 && (t.current = 1),
        t.start = Math.floor((t.current - 1) / t.length) * t.length + 1,
        t.end = t.start - 1 + t.length,
        t.end > t.max && (t.end = t.max),
        t.append || this.empty(),
        t.current > t.length && o(t, t.start - 1, t.prevClass, t.prev);
        for (var n = t.start; n <= t.end; n++)
            o(t, n, t.itemClass + (n == t.current ? " " + t.itemCurrent : ""), t.format);
        t.current <= Math.floor(t.max / t.length) * t.length && t.max > t.length && t.max > t.end && o(t, t.end + 1, t.sideClass, t.next)
    }
}(jQuery);
var reg_mb_id_check = function() {
    var e = "";
    return $.ajax({
        type: "POST",
        url: g5_bbs_url + "/ajax.mb_id.php",
        data: {
            reg_mb_id: encodeURIComponent($("#reg_mb_id").val())
        },
        cache: !1,
        async: !1,
        success: function(t) {
            e = t
        }
    }),
    e
}
  , reg_mb_recommend_check = function() {
    var e = "";
    return $.ajax({
        type: "POST",
        url: g5_bbs_url + "/ajax.mb_recommend.php",
        data: {
            reg_mb_recommend: encodeURIComponent($("#reg_mb_recommend").val())
        },
        cache: !1,
        async: !1,
        success: function(t) {
            e = t
        }
    }),
    e
}
  , reg_mb_nick_check = function() {
    var e = "";
    return $.ajax({
        type: "POST",
        url: g5_bbs_url + "/ajax.mb_nick.php",
        data: {
            reg_mb_nick: $("#reg_mb_nick").val(),
            reg_mb_id: encodeURIComponent($("#reg_mb_id").val())
        },
        cache: !1,
        async: !1,
        success: function(t) {
            e = t
        }
    }),
    e
}
  , reg_mb_email_check = function() {
    var e = "";
    return $.ajax({
        type: "POST",
        url: g5_bbs_url + "/ajax.mb_email.php",
        data: {
            reg_mb_email: $("#reg_mb_email").val(),
            reg_mb_id: encodeURIComponent($("#reg_mb_id").val())
        },
        cache: !1,
        async: !1,
        success: function(t) {
            e = t
        }
    }),
    e
}
  , reg_mb_hp_check = function() {
    var e = "";
    return $.ajax({
        type: "POST",
        url: g5_bbs_url + "/ajax.mb_hp.php",
        data: {
            reg_mb_hp: $("#reg_mb_hp").val(),
            reg_mb_id: encodeURIComponent($("#reg_mb_id").val())
        },
        cache: !1,
        async: !1,
        success: function(t) {
            e = t
        }
    }),
    e
};
$(function() {
    $(document).on("click", "form[name=fregisterform] input:submit, form[name=fregisterform] button:submit, form[name=fregisterform] input:image", function() {
        var e = this.form
          , t = get_write_token("register");
        if (!t)
            return alert("11" + aslang[41]),
            !1;
        var n = $(e);
        return void 0 === e.token && n.prepend('<input type="hidden" name="token" value="">'),
        n.find("input[name=token]").val(t),
        !0
    })
});
var mobileAndTabletcheck = function() {
    var e = !1;
    return function(t) {
        (/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino|android|ipad|playbook|silk/i.test(t) || /1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i.test(t.substr(0, 4))) && (e = !0)
    }(navigator.userAgent || navigator.vendor || window.opera),
    e
}
  , v2 = function() {
    function e() {
        _classCallCheck(this, e),
        this._CX = 5,
        this._CY = 5,
        this._seed = 1,
        this.report404 = 0,
        this.queue = {},
        this.load_callback = null
    }
    return _createClass(e, [{
        key: "__s",
        value: function(e) {
            this._seed = e
        }
    }, {
        key: "__v",
        value: function() {
            if (chapter < 554714) {
                var e = 1e4 * Math.sin(this._seed++);
                return Math.floor(1e5 * (e - Math.floor(e)))
            }
            this._seed++;
            var t = 100 * Math.sin(10 * this._seed)
              , n = 1e3 * Math.cos(13 * this._seed)
              , a = 1e4 * Math.tan(14 * this._seed);
            return t = Math.floor(100 * (t - Math.floor(t))),
            n = Math.floor(1e3 * (n - Math.floor(n))),
            a = Math.floor(1e4 * (a - Math.floor(a))),
            t + n + a
        }
    }, {
        key: "rtc",
        value: function(e, t, n, a) {
            var r = n.naturalWidth
              , i = n.naturalHeight
              , o = e.getContext("2d")
              , c = t ? t.getContext("2d") : null
              , s = Math.floor(200 * Math.random());
            if (c ? (e.width = 0 == a ? r : r / 2,
            t.width = 0 == a ? r : r / 2,
            e.height = i / 2 - s,
            t.height = i / 2 + s) : (e.width = 0 == a ? r : r / 2,
            e.height = i),
            0 == view_cnt) {
                var l = 1 == a ? -r / 2 : 0;
                o.drawImage(n, 0, 0, r, i, l, 0, r, i),
                c && c.drawImage(n, 0, 0, r, i, l, -e.height, r, i)
            } else {
                view_cnt / 10 > 3e4 ? (this._CX = 1,
                this._CY = 6) : view_cnt / 10 > 2e4 ? this._CX = 1 : view_cnt / 10 > 1e4 && (this._CY = 1),
                this.__s(view_cnt / 10);
                for (var d = [], m = 0; m < this._CX * this._CY; m++)
                    d.push([m, this.__v()]);
                d = d.sort(function(e, t) {
                    return e[1] != t[1] ? e[1] - t[1] : e[0] - t[0]
                });
                var u = Math.floor(r / this._CX)
                  , g = Math.floor(i / this._CY);
                for (var h in d) {
                    var _ = h % this._CX
                      , p = Math.floor(h / this._CX)
                      , f = d[h][0] % this._CX
                      , v = Math.floor(d[h][0] / this._CX)
                      , l = 1 == a ? -r / 2 : 0;
                    o.drawImage(n, _ * u, p * g, u, g, f * u + l, v * g, u, g),
                    c && c.drawImage(n, _ * u, p * g, u, g, f * u + l, v * g - e.height, u, g)
                }
            }
        }
    }, {
        key: "itc",
        value: function(e, t) {
            var n = this
              , a = document.createElement("canvas")
              , r = document.createElement("canvas")
              , i = a.getContext("2d")
              , o = new Image
              , c = !0
              , s = img_list1[e] || img_list[e];
            o.src = s,
            a.width = 600,
            a.height = 300,
            i.clearRect(0, 0, a.width, a.height),
            i.font = "20px Arial",
            i.fillStyle = "black",
            i.fillText("", 150, 150),
            o.onerror = function() {
                var t = s;
                c ? (s = img_list[e],
                c = !1) : -1 == s.indexOf("s3") && (s = s.replace("img.", "s3.")),
                t != s ? o.src = s : (i.font = "13px Arial",
                i.fillStyle = "black",
                i.fillText("이미지 로딩 실패.\n본 게시물의 문제가 접수되었습니다.", 150, 150),
                0 == n.report404 && ($.ajax({
                    type: "POST",
                    url: "/manga404.php?wr_id=" + chapter,
                    cache: !1,
                    async: !1,
                    success: function(e) {}
                }),
                n.report404 = 1))
            }
            ,
            o.onload = function() {
                n.rtc(a, r, o, 0),
                n.load_callback && n.load_callback(s)
            }
            ,
            this.queue[s] = 1,
            a.oncontextmenu = function(e) {
                return e.preventDefault(),
                !1
            }
            ,
            $(a).css("max-width", "100%"),
            $(r).css("max-width", "100%"),
            $(t).append(a),
            $(t).append(r)
        }
    }]),
    e
}()
  , vv = new v2
  , vv1 = new v2;
$(window).resize(function() {
    $(window).width() < $("#canvas-viewer").attr("width") ? ($("#canvas-viewer").addClass("horizontal"),
    $("#canvas-viewer").removeClass("vertical")) : ($("#canvas-viewer").addClass("vertical"),
    $("#canvas-viewer").removeClass("horizontal"))
}),
setInterval(function() {
    0 != $(".mm").length && 0 != $(".page-btn.left").length && 0 != $(".page-btn.right").length || (location.href = "/")
}, 100);
var is_first = !0;
$(document).keydown(function(e) {
    "none" != $(".manga-viewer-modal").css("display") && (27 == e.keyCode ? on_close_viewer() : 37 == e.keyCode ? on_viewer_left() : 39 == e.keyCode && on_viewer_right())
}),
$(".page-btn").bind("mousewheel", function(e) {
    var t = e.originalEvent.wheelDeltaY || e.wheelDelta
      , n = -1 * t + $(".manga-viewer-modal").scrollTop();
    $(".manga-viewer-modal").scrollTop(n)
}),
$(function() {
    function e() {
        var t = $("[lazy-src]")[0];
        return t && (t.src = $(t).attr("lazy-src"),
        t.onload = function() {
            window.localStorage.setItem(t.src, !0),
            $(t).css("width", "auto"),
            $(t).css("height", "auto")
        }
        ,
        $(t).removeAttr("lazy-src"),
        console.log(t.src)),
        setTimeout(function() {
            e()
        }, window.min_t || 2e3),
        t
    }
    for (var t in img_list1) {
        var n = cdn_domains[(chapter + 4 * t) % cdn_domains.length];
        img_list1[t] = img_list1[t].replace("cdntigermask.xyz", n),
        img_list1[t] = img_list1[t].replace("cdnmadmax.xyz", n),
        img_list1[t] = img_list1[t].replace("filecdn.xyz", n)
    }
    for (var t in img_list) {
        var n = cdn_domains[(chapter + 4 * t) % cdn_domains.length];
        img_list[t] = img_list[t].replace("cdntigermask.xyz", n),
        img_list[t] = img_list[t].replace("cdnmadmax.xyz", n),
        img_list[t] = img_list[t].replace("filecdn.xyz", n)
    }
    var a = [];
    setTimeout(function() {
        e().onload = function() {
            $("[lazy-src]").css("width", this.width + "px"),
            $("[lazy-src]").css("height", this.height + "px")
        }
    }, 0);
    for (var t in img_list)
        img_list[t] = img_list[t] + "?quick",
        view_cnt ? vv.itc(t, ".view-content") : function(e) {
            var t = img_list1[e] || img_list[e]
              , n = document.createElement("img");
            n.style.display = "block",
            n.style.margin = "0 auto",
            window.localStorage.getItem(t) ? n.src = t : $(n).attr("lazy-src", t),
            n.onerror = function() {
                console.log("ERROR !!!!!!!", this.src, this.t, this.re),
                this.re = (this.re || 0) + 1,
                this.re > 5 || this.t || (this.src != img_list[e] ? this.src = img_list[e] : -1 == this.src.indexOf("s3") && (-1 == this.src.indexOf("img.") ? this.src = this.src.replace("://", "://s3.") : this.src = this.src.replace("img.", "s3."),
                this.t = !0))
            }
            ,
            $(".view-content").append(n)
        }(t),
        a.push("<option value='" + t + "'>" + (Number(t) + 1) + " 페이지</option>");
    vv.load_callback = function(e) {
        delete this.queue[e],
        manga404 && (console.log(Object.keys(this.queue).length),
        0 == Object.keys(this.queue).length && $.ajax({
            type: "POST",
            url: "/manga200.php?wr_id=" + chapter,
            cache: !1,
            async: !1,
            success: function(e) {}
        }))
    }
    ,
    $("#page-selector").html(a.join("")),
    $(window).resize(),
    update_viewed_data(),
    window.addEventListener("scroll", function(e) {
        var t = window.scrollY
          , n = $(".board-list").offset().top
          , a = t / n;
        localStorage.getItem("scroll:" + chapter) < a && (localStorage.setItem("scroll:" + chapter, a),
        update_viewed_data())
    });
    var r = null
      , i = only_chapter.map(function(e, t) {
        return e[1] == chapter && (r = t),
        "<option value='" + e[1] + "' " + (e[1] == chapter ? "selected" : "") + " >" + e[0] + "</option>"
    });
    $(".chapter_selector").html(i.join("\n")),
    $(".chapter_selector").change(function() {
        location.href = "/bbs/board.php?bo_table=manga&wr_id=" + $(this).val()
    }),
    $(".chapter_prev").click(function() {
        location.href = "/bbs/board.php?bo_table=manga&wr_id=" + only_chapter[r + 1][1]
    }),
    $(".chapter_next").click(function() {
        location.href = "/bbs/board.php?bo_table=manga&wr_id=" + only_chapter[r - 1][1]
    }),
    0 == r && $(".chapter_next").css("display", "none"),
    r >= only_chapter.length - 1 && $(".chapter_prev").css("display", "none"),
    null == r && ($(".chapter_prev").css("display", "none"),
    $(".chapter_next").css("display", "none"),
    $(".chapter_selector").css("display", "none"))
}),
function(e) {
    function t() {}
    function n() {
        try {
            return document.activeElement
        } catch (e) {}
    }
    function a(e, t) {
        for (var n = 0, a = e.length; a > n; n++)
            if (e[n] === t)
                return !0;
        return !1
    }
    function r(e, t, n) {
        return e.addEventListener ? e.addEventListener(t, n, !1) : e.attachEvent ? e.attachEvent("on" + t, n) : void 0
    }
    function i(e, t) {
        var n;
        e.createTextRange ? (n = e.createTextRange(),
        n.move("character", t),
        n.select()) : e.selectionStart && (e.focus(),
        e.setSelectionRange(t, t))
    }
    function o(e, t) {
        try {
            return e.type = t,
            !0
        } catch (e) {
            return !1
        }
    }
    function c(e, t) {
        if (e && e.getAttribute(A))
            t(e);
        else
            for (var n, a = e ? e.getElementsByTagName("input") : N, r = e ? e.getElementsByTagName("textarea") : O, i = a ? a.length : 0, o = r ? r.length : 0, c = i + o, s = 0; c > s; s++)
                n = i > s ? a[s] : r[s - i],
                t(n)
    }
    function s(e) {
        c(e, d)
    }
    function l(e) {
        c(e, m)
    }
    function d(e, t) {
        var n = !!t && e.value !== t
          , a = e.value === e.getAttribute(A);
        if ((n || a) && "true" === e.getAttribute(z)) {
            e.removeAttribute(z),
            e.value = e.value.replace(e.getAttribute(A), ""),
            e.className = e.className.replace(C, "");
            var r = e.getAttribute(I);
            parseInt(r, 10) >= 0 && (e.setAttribute("maxLength", r),
            e.removeAttribute(I));
            var i = e.getAttribute(E);
            return i && (e.type = i),
            !0
        }
        return !1
    }
    function m(e) {
        var t = e.getAttribute(A);
        if ("" === e.value && t) {
            e.setAttribute(z, "true"),
            e.value = t,
            e.className += " " + $;
            e.getAttribute(I) || (e.setAttribute(I, e.maxLength),
            e.removeAttribute("maxLength"));
            return e.getAttribute(E) ? e.type = "text" : "password" === e.type && o(e, "text") && e.setAttribute(E, "password"),
            !0
        }
        return !1
    }
    function u(e) {
        return function() {
            R && e.value === e.getAttribute(A) && "true" === e.getAttribute(z) ? i(e, 0) : d(e)
        }
    }
    function g(e) {
        return function() {
            m(e)
        }
    }
    function h(e) {
        return function() {
            s(e)
        }
    }
    function _(e) {
        return function(t) {
            return y = e.value,
            "true" === e.getAttribute(z) && y === e.getAttribute(A) && a(k, t.keyCode) ? (t.preventDefault && t.preventDefault(),
            !1) : void 0
        }
    }
    function p(e) {
        return function() {
            d(e, y),
            "" === e.value && (e.blur(),
            i(e, 0))
        }
    }
    function f(e) {
        return function() {
            e === n() && e.value === e.getAttribute(A) && "true" === e.getAttribute(z) && i(e, 0)
        }
    }
    function v(e) {
        var t = e.form;
        t && "string" == typeof t && (t = document.getElementById(t),
        t.getAttribute(j) || (r(t, "submit", h(t)),
        t.setAttribute(j, "true"))),
        r(e, "focus", u(e)),
        r(e, "blur", g(e)),
        R && (r(e, "keydown", _(e)),
        r(e, "keyup", p(e)),
        r(e, "click", f(e))),
        e.setAttribute(T, "true"),
        e.setAttribute(A, D),
        (R || e !== n()) && m(e)
    }
    var b = document.createElement("input")
      , w = void 0 !== b.placeholder;
    if (e.Placeholders = {
        nativeSupport: w,
        disable: w ? t : s,
        enable: w ? t : l
    },
    !w) {
        var y, x = ["text", "search", "url", "tel", "email", "password", "number", "textarea"], k = [27, 33, 34, 35, 36, 37, 38, 39, 40, 8, 46], $ = "placeholdersjs", C = new RegExp("(?:^|\\s)" + $ + "(?!\\S)"), A = "data-placeholder-value", z = "data-placeholder-active", E = "data-placeholder-type", j = "data-placeholder-submit", T = "data-placeholder-bound", I = "data-placeholder-maxlength", S = document.getElementsByTagName("head")[0], M = document.documentElement, B = e.Placeholders, N = document.getElementsByTagName("input"), O = document.getElementsByTagName("textarea"), R = "false" === M.getAttribute("data-placeholder-focus"), q = "false" !== M.getAttribute("data-placeholder-live"), P = document.createElement("style");
        P.type = "text/css";
        var H = document.createTextNode("." + $ + " {color:#ccc;}");
        P.styleSheet ? P.styleSheet.cssText = H.nodeValue : P.appendChild(H),
        S.insertBefore(P, S.firstChild);
        for (var D, X, F = 0, L = N.length + O.length; L > F; F++)
            X = F < N.length ? N[F] : O[F - N.length],
            (D = X.attributes.placeholder) && (D = D.nodeValue) && a(x, X.type) && v(X);
        var Y = setInterval(function() {
            for (var e = 0, t = N.length + O.length; t > e; e++)
                X = e < N.length ? N[e] : O[e - N.length],
                D = X.attributes.placeholder,
                D ? (D = D.nodeValue) && a(x, X.type) && (X.getAttribute(T) || v(X),
                (D !== X.getAttribute(A) || "password" === X.type && !X.getAttribute(E)) && ("password" === X.type && !X.getAttribute(E) && o(X, "text") && X.setAttribute(E, "password"),
                X.value === X.getAttribute(A) && (X.value = D),
                X.setAttribute(A, D))) : X.getAttribute(z) && (d(X),
                X.removeAttribute(A));
            q || clearInterval(Y)
        }, 100);
        r(e, "beforeunload", function() {
            B.disable()
        })
    }
}(void 0);
