define([
    'jquery',
    'backbone',
    'underscore',
    'views/alerts',
    'text!templates/commandPopup.html','datepicker'],
    function($, Backbone, _,alerts, template){
        var View = Backbone.View.extend({
            className: "modal hide ",
            initialize: function(){
                _(this).bindAll('change');
            },
            events:{
                "change":"change" ,
                "changeDate":"change",
                'click .ok'	: '_handleOk',
                'click .cancel'	: '_handleCancel'
            },
            _handleOk: function(event){
                event.preventDefault();
                this.ok();
            },
            _handleCancel: function(event){
                event.preventDefault();
                this.cancel();
            },
            ok: function(){
                this.hide();
                this.deferred.resolve();
            },
            cancel: function(){
                this.hide();
                this.deferred.reject();
            },
            change: function(event){
                var target = event.target;
                var parameter= _.find(this.command.Parameters,function(v){return v.Name==target.name});
                if(event.target.type=="checkbox")
                    parameter.Value=target.value!="";
                else
                    parameter.Value=target.value;
            },
            open: function(cmd){
                this.command=$.extend(true,{},cmd,{});

                var deferred = this.deferred = new $.Deferred;
                this.template = _.template( template, this.command );
                $(this.el).html(this.template);
                $(this.el).find('.datepicker').datepicker({autoclose:true,format:"dd/mm/yyyy"});
                $('body').append(this.el);

                this.$el.modal({
                    keyboard:false,
                    backdrop:false
                });

                $('.modal-backdrop').off('click');

                return deferred;
            },
            hide: function(){
                this.$el.modal('hide');
            },
            'dispose':function(){

            }
        });

        return new View();
    });
