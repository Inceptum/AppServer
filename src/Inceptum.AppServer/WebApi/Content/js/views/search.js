define([
        'jquery',
        'backbone',
        'underscore'
    ],
    function ($, Backbone) {

        function escapeRegExp(str) {
            return str.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
        }

        var View = Backbone.View.extend({
            'dispose':function() {
                
            },
            render: function() {
                var maxCount = 16;
                $('#search').typeahead({
                    minLength: 1,
                    items: maxCount,
                    matcher: function() {
                        return true;
                    },
                    highlighter: function(item) {
                        var query = this.query;

                        var regexp = new RegExp(escapeRegExp(query), 'gi');
                        var hl = function(x) {
                            return x.replace(regexp, function (str) { return '<b style="text-decoration: underline;">' + str + '</b>' });
                        };

                        var html = '<div class="typeahead" style="font-size: 55%;">';
                        html += '<div class="pull-left margin-small">';
                        html += '<div class="text-left typeahead-head">' + hl(item.configuration) + ' / ' + hl(item.id) + '</div>';
                        html += '<div class="text-left">' + hl(item.content) + '</div>';
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
                            }
                        });
                    }
                });

                return this;
            }
        });

        return View;
    });