define([
        'jquery',
        'backbone',
        'underscore'
    ],
    function ($, Backbone) {

        var View = Backbone.View.extend({
            autosize: function () {
                var that = this;
                setTimeout(function () {
                    that.$el.parent().find('ul.typeahead.dropdown-menu').css({
                        width: $(window).width() / 3
                    });
                }, 0);
            },
            render: function () {
                var that = this;
                var maxCount = 16;
                this.$el.typeahead({
                    minLength: 1,
                    items: maxCount,
                    matcher: function() {
                        return true;
                    },
                    highlighter: function(item) {
                        var html = '<div class="typeahead">';
                        html += '<div class="pull-left margin-small">';
                        html += '<div class="text-left typeahead-head">' + item.configuration + ' / ' + item.id + '</div>';
                        html += '<div class="text-left">' + item.content + '</div>';
                        html += '</div>';
                        html += '<div class="clearfix"></div>';
                        html += '</div>';
                        return html;
                    },
                    sorter: function(items) {
                        return items;
                    },
                    updater: function(item) {
                        location.href = '#/configurations/' + item.configuration + '/bundles/' + item.id;
                        return '';
                    },
                    source: function(query, process) {
                        $.ajax({
                            url: "/api/configurations/search",
                            type: 'GET',
                            data: { term: query, maxCount: maxCount },
                            dataType: 'json',
                            success: function(data) {
                                process(data);
                                that.autosize();
                            }
                        });
                    }
                });

                return this;
            }
        });

        return View;
    });