// =========================================
// Job Recruitment System - Main JavaScript
// =========================================

(function ($) {
    'use strict';

    // =========================================
    // Document Ready
    // =========================================
    $(document).ready(function () {
        initializeTooltips();
        initializePopovers();
        initializeFormValidation();
        initializeAlertDismiss();
        initializeConfirmDialogs();
        initializeSearchDebounce();
        initializeBookmarkButtons();
    });

    // =========================================
    // Bootstrap Components Initialization
    // =========================================
    
    function initializeTooltips() {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    function initializePopovers() {
        var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }

    // =========================================
    // Form Validation Enhancement
    // =========================================
    
    function initializeFormValidation() {
        // Add Bootstrap validation classes on blur
        $('form').on('blur', 'input, select, textarea', function () {
            var $input = $(this);
            if ($input.hasClass('input-validation-error')) {
                $input.addClass('is-invalid').removeClass('is-valid');
            } else if ($input.val()) {
                $input.addClass('is-valid').removeClass('is-invalid');
            }
        });

        // Clear validation on input
        $('form').on('input', 'input, select, textarea', function () {
            var $input = $(this);
            if (!$input.hasClass('input-validation-error')) {
                $input.removeClass('is-invalid');
            }
        });
    }

    // =========================================
    // Alert Auto-Dismiss
    // =========================================
    
    function initializeAlertDismiss() {
        // Auto-dismiss success alerts after 5 seconds
        $('.alert-success[data-auto-dismiss]').each(function () {
            var $alert = $(this);
            var timeout = parseInt($alert.data('auto-dismiss')) || 5000;
            setTimeout(function () {
                $alert.fadeOut(300, function () {
                    $(this).remove();
                });
            }, timeout);
        });
    }

    // =========================================
    // Confirmation Dialogs
    // =========================================
    
    function initializeConfirmDialogs() {
        $(document).on('click', '[data-confirm]', function (e) {
            var message = $(this).data('confirm') || 'Are you sure you want to proceed?';
            if (!confirm(message)) {
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        });
    }

    // =========================================
    // Search Debounce
    // =========================================
    
    function initializeSearchDebounce() {
        var searchTimeout;
        $('.search-input[data-search-url]').on('input', function () {
            var $input = $(this);
            var url = $input.data('search-url');
            var $results = $($input.data('search-results'));
            
            clearTimeout(searchTimeout);
            
            searchTimeout = setTimeout(function () {
                var query = $input.val();
                if (query.length >= 2) {
                    $.get(url, { q: query }, function (data) {
                        $results.html(data);
                    });
                } else {
                    $results.empty();
                }
            }, 300);
        });
    }

    // =========================================
    // Bookmark Functionality (AJAX-based)
    // =========================================
    
    function initializeBookmarkButtons() {
        // Handle all bookmark buttons (both in job listings and detail pages)
        $(document).on('click', '.btn-bookmark, .btn-bookmark-detail', function (e) {
            e.preventDefault();
            e.stopPropagation();
            
            var $btn = $(this);
            var jobId = $btn.data('job-id');
            
            if (!jobId) {
                console.error('No job ID found for bookmark button');
                return;
            }
            
            // Prevent double-clicking
            if ($btn.hasClass('bookmark-loading')) return;
            $btn.addClass('bookmark-loading');
            
            // Get CSRF token
            var token = $('input[name="__RequestVerificationToken"]').val();
            if (!token) {
                // Try to find it in a meta tag or create one
                token = $('meta[name="csrf-token"]').attr('content');
            }
            
            // Add loading animation
            var $icon = $btn.find('i');
            var originalIconClass = $icon.attr('class');
            $icon.attr('class', 'bi bi-arrow-repeat bookmark-spin');
            
            $.ajax({
                url: '/api/jobs/bookmark',
                method: 'POST',
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': token
                },
                data: JSON.stringify({ jobVacancyId: jobId }),
                success: function (response) {
                    $btn.removeClass('bookmark-loading');
                    
                    if (response.success) {
                        // Update button state
                        if (response.isBookmarked) {
                            $icon.attr('class', 'bi bi-bookmark-fill');
                            $btn.addClass('bookmarked');
                            $btn.attr('title', 'Remove bookmark');
                            
                            // Update text if it's a detail page button
                            if ($btn.hasClass('btn-bookmark-detail')) {
                                $btn.removeClass('btn-outline-secondary').addClass('btn-warning');
                                $btn.html('<i class="bi bi-bookmark-fill me-2"></i>Saved');
                            }
                            
                            // Show toast
                            JobRecruitment.showToast('Job saved to bookmarks!', 'success');
                        } else {
                            $icon.attr('class', 'bi bi-bookmark');
                            $btn.removeClass('bookmarked');
                            $btn.attr('title', 'Bookmark this job');
                            
                            // Update text if it's a detail page button
                            if ($btn.hasClass('btn-bookmark-detail')) {
                                $btn.removeClass('btn-warning').addClass('btn-outline-secondary');
                                $btn.html('<i class="bi bi-bookmark me-2"></i>Save for Later');
                            }
                            
                            // Show toast
                            JobRecruitment.showToast('Job removed from bookmarks', 'info');
                        }
                        
                        // Add click animation
                        $btn.addClass('bookmark-clicked');
                        setTimeout(function() {
                            $btn.removeClass('bookmark-clicked');
                        }, 300);
                    } else {
                        $icon.attr('class', originalIconClass);
                        JobRecruitment.showToast(response.message || 'Failed to update bookmark', 'danger');
                    }
                },
                error: function (xhr) {
                    $btn.removeClass('bookmark-loading');
                    $icon.attr('class', originalIconClass);
                    
                    if (xhr.status === 401) {
                        JobRecruitment.showToast('Please log in to save jobs', 'warning');
                    } else {
                        JobRecruitment.showToast('An error occurred. Please try again.', 'danger');
                    }
                }
            });
        });
    }

    // =========================================
    // Utility Functions
    // =========================================
    
    window.JobRecruitment = {
        // Show loading overlay
        showLoading: function () {
            if (!$('#loading-overlay').length) {
                $('body').append('<div id="loading-overlay" class="spinner-overlay"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>');
            }
            $('#loading-overlay').fadeIn(150);
        },

        // Hide loading overlay
        hideLoading: function () {
            $('#loading-overlay').fadeOut(150);
        },

        // Show toast notification
        showToast: function (message, type) {
            type = type || 'info';
            var toastHtml = '<div class="toast align-items-center text-white bg-' + type + ' border-0" role="alert" aria-live="assertive" aria-atomic="true">' +
                '<div class="d-flex">' +
                '<div class="toast-body">' + message + '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
                '</div></div>';

            if (!$('#toast-container').length) {
                $('body').append('<div id="toast-container" class="toast-container position-fixed bottom-0 end-0 p-3"></div>');
            }

            var $toast = $(toastHtml);
            $('#toast-container').append($toast);
            var toast = new bootstrap.Toast($toast[0], { autohide: true, delay: 5000 });
            toast.show();

            $toast.on('hidden.bs.toast', function () {
                $(this).remove();
            });
        },

        // Format currency
        formatCurrency: function (amount, currency) {
            currency = currency || 'USD';
            return new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: currency
            }).format(amount);
        },

        // Format date
        formatDate: function (dateString, format) {
            var date = new Date(dateString);
            var options = format || { year: 'numeric', month: 'short', day: 'numeric' };
            return date.toLocaleDateString('en-US', options);
        },

        // Truncate text
        truncateText: function (text, maxLength) {
            if (text.length <= maxLength) return text;
            return text.substring(0, maxLength) + '...';
        },

        // AJAX form submission
        submitFormAjax: function (formSelector, options) {
            var defaults = {
                onSuccess: function (response) { },
                onError: function (xhr) { },
                showLoading: true
            };
            var settings = $.extend({}, defaults, options);

            $(formSelector).on('submit', function (e) {
                e.preventDefault();
                var $form = $(this);

                if (settings.showLoading) {
                    JobRecruitment.showLoading();
                }

                $.ajax({
                    url: $form.attr('action'),
                    method: $form.attr('method') || 'POST',
                    data: $form.serialize(),
                    success: function (response) {
                        if (settings.showLoading) {
                            JobRecruitment.hideLoading();
                        }
                        settings.onSuccess(response);
                    },
                    error: function (xhr) {
                        if (settings.showLoading) {
                            JobRecruitment.hideLoading();
                        }
                        settings.onError(xhr);
                    }
                });
            });
        }
    };

    // =========================================
    // Chart.js Helper Functions
    // =========================================
    
    window.ChartHelper = {
        // Default chart colors
        colors: {
            primary: '#2563eb',
            secondary: '#64748b',
            success: '#10b981',
            warning: '#f59e0b',
            danger: '#ef4444',
            info: '#06b6d4'
        },

        // Create a bar chart
        createBarChart: function (canvasId, labels, data, options) {
            var ctx = document.getElementById(canvasId);
            if (!ctx) return null;

            var defaults = {
                label: 'Data',
                backgroundColor: this.colors.primary,
                borderRadius: 4
            };
            var settings = $.extend({}, defaults, options);

            return new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: settings.label,
                        data: data,
                        backgroundColor: settings.backgroundColor,
                        borderRadius: settings.borderRadius
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    }
                }
            });
        },

        // Create a doughnut chart
        createDoughnutChart: function (canvasId, labels, data, colors) {
            var ctx = document.getElementById(canvasId);
            if (!ctx) return null;

            colors = colors || Object.values(this.colors);

            return new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: data,
                        backgroundColor: colors,
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    },
                    cutout: '60%'
                }
            });
        },

        // Create a line chart
        createLineChart: function (canvasId, labels, datasets) {
            var ctx = document.getElementById(canvasId);
            if (!ctx) return null;

            return new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: datasets
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'top'
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    elements: {
                        line: {
                            tension: 0.3
                        }
                    }
                }
            });
        }
    };

})(jQuery);
