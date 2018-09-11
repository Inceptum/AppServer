define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/instanceRow.html'],
    function($, Backbone, _, template){
        var View = Backbone.View.extend({
            tagName:"tr",
            initialize: function(){
                _.bindAll(this, "render" ,"emitCommand");
                this.model.bind('change', this.render);
            },
            events:{
                "click .start":"start",
                "click .debug": "debug",
                "click .kill": "kill",
                "click .stop":"stop",
                "click .restart":"restart",
                "click .delete":"delete",
                "click .command":"emitCommand"
            },

            'delete': function (e) {
                e.preventDefault();
                if ($(this.el).find(".delete").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled','disabled');
                this.trigger('destroy',this.model,this);
            },
            stop:function(e){
                e.preventDefault();
                if ($(this.el).find(".stop").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled', 'disabled');
                this.trigger('stop',this.model,this);
            },
            start:function(e){
                e.preventDefault();
                if ($(this.el).find(".start").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled', 'disabled');
                this.trigger('start',this.model,this);
            },
            debug: function (e) {
                e.preventDefault();
                if ($(this.el).find(".debug").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled', 'disabled');
                this.trigger('debug', this.model, this);
            },
            restart:function(e){
                e.preventDefault();
                if ($(this.el).find(".restart").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled','disabled');
                this.trigger('restart',this.model,this);
            },
            kill: function (e) {
                e.preventDefault();
                if ($(this.el).find(".kill").attr('disabled')) return false;
                $(this.el).find(".actions button").addClass('disabled').attr('disabled','disabled');
                this.trigger('kill', this.model, this);
            },
            emitCommand:function(e){
                console.log($(e.target).data('command')) ;
                e.preventDefault();
                $(this.el).find(".actions button").addClass('disabled').attr('disabled','disabled');
                this.trigger("command",this.model,this,$(e.target).data('command'));
            },
            render: function(){
                this.template = _.template( template, { model: this.model.toJSON() } );
                $(this.el).html(this.template);
                $(this.el).find(".actions .btn.cmd").addClass('disabled').attr('disabled','disabled');
                $(this.el).find(".kill").removeClass('disabled').removeAttr('disabled', 'disabled');
                

                if(this.model.get("status")=="Started")
                {
                    $(this.el).find(".restart").removeClass('disabled').removeAttr('disabled','disabled');
                    $(this.el).find(".stop").removeClass('disabled').removeAttr('disabled', 'disabled');
                    $(this.el).find(".debug").removeClass('disabled').removeAttr('disabled', 'disabled');
                }
                if(this.model.get("status")=="Stopped")
                {
                    $(this.el).find(".debug").removeClass('disabled').removeAttr('disabled','disabled');
                    $(this.el).find(".start").removeClass('disabled').removeAttr('disabled','disabled');
                    $(this.el).find(".delete").removeClass('disabled').removeAttr('disabled','disabled');
                    $(this.el).find(".kill").addClass('disabled').attr('disabled', 'disabled');
                }
                $(this.el).find(".commands").removeClass('disabled').removeAttr('disabled','disabled');

                return this;
            },
            'dispose':function(){
                this.model.unbind('change', this.render);
            }
        });

        return View;
    });
