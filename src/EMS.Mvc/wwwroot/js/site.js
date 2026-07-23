/**
 * EMS Student Portal — site.js (Aplot AI Style Enabled)
 * Integrated with:
 * - Lenis Smooth Scroll
 * - GSAP & ScrollTrigger Motion Physics & Section Snapping
 * - Dark/Light Theme Toggle & Interactivity
 */

(function () {
    'use strict';

    /* ─── Dark Mode Toggle ────────────────────────────────────── */
    var THEME_KEY = 'ems-theme';
    var DARK      = 'dark';
    var LIGHT     = 'light';

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);

        var btn   = document.getElementById('themeToggleBtn');
        var icon  = document.getElementById('themeToggleIcon');
        var label = document.getElementById('themeToggleLabel');

        if (!btn) return;

        if (theme === DARK) {
            if (icon)  icon.className  = 'ri-sun-line';
            if (label) label.textContent = 'Sáng';
            btn.setAttribute('aria-label', 'Chuyển sang chế độ sáng');
            btn.setAttribute('title', 'Chuyển sang chế độ sáng');
        } else {
            if (icon)  icon.className  = 'ri-moon-line';
            if (label) label.textContent = 'Tối';
            btn.setAttribute('aria-label', 'Chuyển sang chế độ tối');
            btn.setAttribute('title', 'Chuyển sang chế độ tối');
        }
    }

    function toggleTheme() {
        var current = document.documentElement.getAttribute('data-theme') || LIGHT;
        applyTheme(current === DARK ? LIGHT : DARK);
    }

    /* ─── Lenis Smooth Scroll & GSAP ScrollTrigger Integration ─── */
    function initSmoothScrollAndAnimations() {
        if (typeof Lenis === 'undefined' || typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') {
            console.warn('Lenis or GSAP libraries not loaded yet.');
            return;
        }

        // Register GSAP ScrollTrigger plugin
        gsap.registerPlugin(ScrollTrigger);

        // 1. Initialize Lenis Smooth Scroll
        var lenis = new Lenis({
            duration: 1.2,
            easing: function (t) { return Math.min(1, 1.001 - Math.pow(2, -10 * t)); },
            orientation: 'vertical',
            gestureOrientation: 'vertical',
            smoothWheel: true,
            wheelMultiplier: 1,
            touchMultiplier: 1.8,
            infinite: false,
        });

        // Synchronize Lenis scroll position with GSAP ScrollTrigger
        lenis.on('scroll', ScrollTrigger.update);

        gsap.ticker.add(function (time) {
            lenis.raf(time * 1000);
        });

        gsap.ticker.lagSmoothing(0);

        // Expose lenis globally for optional manual scroll calls
        window.emsLenis = lenis;

        // 2. GSAP ScrollTrigger Proximity Snap (Disabled on Mobile <768px)
        var isMobileView = window.innerWidth < 768 || ('ontouchstart' in window && window.innerWidth < 992);
        var snapSections = gsap.utils.toArray('section, footer');
        if (snapSections.length > 0 && !isMobileView) {
            ScrollTrigger.create({
                trigger: document.body,
                start: 'top top',
                end: 'bottom bottom',
                snap: {
                    snapTo: function (progress) {
                        if (window.innerWidth < 768) return progress;
                        var maxScroll = ScrollTrigger.maxScroll(window);
                        if (!maxScroll) return progress;

                        var sectionPositions = snapSections.map(function (sec) {
                            return (sec.offsetTop || 0) / maxScroll;
                        });

                        var closest = sectionPositions.reduce(function (prev, curr) {
                            return Math.abs(curr - progress) < Math.abs(prev - progress) ? curr : prev;
                        });

                        // Proximity threshold: snap only if within 8% of section boundary
                        var proximityThreshold = 0.08;
                        if (Math.abs(closest - progress) < proximityThreshold) {
                            return closest;
                        }
                        return progress;
                    },
                    duration: { min: 0.2, max: 0.5 },
                    delay: 0.1,
                    ease: 'power1.inOut'
                }
            });
        }

        // 3. GSAP ScrollTrigger Entrance Animations
        // Hero Section Animations
        var heroTitle = document.querySelector('.heading-gradient-lg');
        var heroSubtitle = document.querySelector('.subtitle-text');
        var inputDock = document.querySelector('.aplot-dock-card');

        if (heroTitle) {
            gsap.from(heroTitle, {
                y: 35,
                opacity: 0,
                duration: 1,
                ease: 'power3.out',
                delay: 0.1
            });
        }

        if (heroSubtitle) {
            gsap.from(heroSubtitle, {
                y: 25,
                opacity: 0,
                duration: 0.9,
                ease: 'power3.out',
                delay: 0.25
            });
        }

        if (inputDock) {
            gsap.from(inputDock, {
                y: 40,
                scale: 0.96,
                opacity: 0,
                duration: 1.1,
                ease: 'back.out(1.2)',
                delay: 0.4
            });
        }

        // Stat Cards Stagger Reveal (Targets outer container .stat-card-wrapper)
        var statCards = document.querySelectorAll('.stat-card-wrapper');
        if (statCards.length > 0) {
            gsap.from(statCards, {
                scrollTrigger: {
                    trigger: statCards[0],
                    start: 'top 90%',
                    toggleActions: 'play none none none'
                },
                y: 30,
                opacity: 0,
                duration: 0.75,
                stagger: 0.1,
                ease: 'power3.out',
                clearProps: 'transform,opacity'
            });
        }

        // Event Cards Stagger Reveal
        var eventCards = document.querySelectorAll('.event-glass-card');
        if (eventCards.length > 0) {
            gsap.from(eventCards, {
                scrollTrigger: {
                    trigger: eventCards[0],
                    start: 'top 88%',
                    toggleActions: 'play none none none'
                },
                y: 30,
                opacity: 0,
                duration: 0.8,
                stagger: 0.12,
                ease: 'power3.out',
                clearProps: 'transform,opacity'
            });
        }
    }

    /* ─── DOM Ready Handlers ───────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', function () {
        // Apply saved theme on load
        var saved = localStorage.getItem(THEME_KEY) || LIGHT;
        applyTheme(saved);

        var btn = document.getElementById('themeToggleBtn');
        if (btn) {
            btn.addEventListener('click', toggleTheme);
        }

        // Initialize Lenis + GSAP ScrollTrigger
        initSmoothScrollAndAnimations();

        // Initialize Smart Auto-Hiding Navbar
        initSmartHeader();
    });

    /* ─── Smart Auto-Hiding Floating Glass Header ───────────────── */
    function initSmartHeader() {
        var header = document.querySelector('.floating-header-wrap');
        if (!header) return;

        var lastScrollY = 0;
        var threshold = 12;

        function onScrollUpdate(currentY) {
            if (currentY <= 60) {
                header.classList.remove('header-hidden');
                lastScrollY = currentY;
                return;
            }

            var diff = currentY - lastScrollY;
            if (Math.abs(diff) > threshold) {
                if (diff > 0 && currentY > 120) {
                    // Scroll DOWN -> slide header up offscreen
                    header.classList.add('header-hidden');
                } else if (diff < 0) {
                    // Scroll UP -> reveal header smoothly
                    header.classList.remove('header-hidden');
                }
                lastScrollY = currentY;
            }
        }

        if (window.emsLenis) {
            window.emsLenis.on('scroll', function (e) {
                onScrollUpdate(e.scroll);
            });
        }
        
        window.addEventListener('scroll', function () {
            onScrollUpdate(window.scrollY || document.documentElement.scrollTop);
        }, { passive: true });
    }

})();

