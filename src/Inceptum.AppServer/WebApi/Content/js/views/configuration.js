define([
    'jquery',
    'backbone',
    'underscore',
    'views/confirm',
    'views/alerts',
    'text!templates/configuration/configuration.html',
    'bootbox',
    'fileupload'],
    function($, Backbone, _, confirmView,alerts, template){
        var View = Backbone.View.extend({
            el:'#content',
            initialize: function(){
//                console.log("!!!");

                _.bindAll(this,"upload","dispose");
            },
            events:{
                "click #delete":"destroy",
                "click #import":"upload",
                "click #importSubmit":"doImport"
            },
            upload:function(){

                var input = $('#inputFile').unbind();
                input.after(input.clone(true).change(function() {
                    $('#fakeInputFile').val($(this).val());
                })).remove();
                $('#fakeInputFile').val("");
                $('#progress .bar').css('width','0%');
                var self=this;
                this.uploader=this.importDialog.find("#inputFile").fileupload({
                   /* dataType: 'json',*/
                    multipart:true,
                    autoUpload:false,
                    fileInput:null,
                    paramName:"file",
                    done: function (e, data) {
                        $('#progressAlert').addClass("hide");
                        self.model.fetch({async:false});
                        self.render();
                    },
                    fail: function (e, data) {
                        $('#progressAlert').addClass("hide");
                        self.model.fetch({async:false});
                        self.render();
                    },
                    progressall: function (e, data) {
                        var progress = parseInt(data.loaded / data.total * 100, 10);
                        $('#progress .bar').css(
                            'width',
                            progress + '%'
                        );
                    }
                });
                this.importDialog.modal();
            },
            doImport:function(){
                $('#progressAlert').removeClass("hide");
                this.importDialog.modal('hide');
                this.uploader.fileupload('send',{fileInput: this.uploader});
            },
            destroy:function(){
                var self=this;
                confirmView.open({title:"Delete",body:"You are about to delete '"+this.model.id+"' configuration. Are you sure?",confirm_text:"Delete"})
                    .done(function(){
                        self.model.destroy({
                            wait: true,
                            success:function(){
                                alerts.show({
                                    type:"info",
                                    text:"Configuration '"+self.model.id+"' deleted."});
                                self.navigate('#/configurations', true);
                            },
                            error:function(model,response){
                                alerts.show({
                                    type:"error",
                                    text:"Failed to delete configuration '"+self.model.id+"'. "+JSON.parse(response.responseText).Error});
                            }
                        });
                    });
            },
            render: function(){
                this.template = _.template( template, {model: this.model.toJSON() } );
                $(this.el).html(this.template);

                this.importDialog=$("#importDialog");
                $('#fakeInputFile').val("").next().click(function(){$('#inputFile').click();});

                return this;
            },
            'dispose':function(){

            }
        });

        return View;
    });
