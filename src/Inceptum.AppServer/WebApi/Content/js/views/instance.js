define([
        'jquery',
        'backbone',
        'underscore',
        'text!templates/instance.html',
        'views/serverSideBar'
    ],
    function($, Backbone, _, template, ServerSideBarView) {
        var View = Backbone.View.extend({
            el: '#content',
            initialize: function() {
            },
            render: function() {
                this.template = _.template(template, { model: this.model.toJSON() });
                $(this.el).html(this.template);
                this.sidebar = new ServerSideBarView({ el: $(this.el).find(".sidebar"), active: "serverStatus" });
                this.sidebar.render();
            }
        });

        return View;
    });