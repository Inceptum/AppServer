define([
    'jquery',
    'backbone',
    'underscore',
    'text!templates/bundle.html',
    'views/alerts','libs/prettify',
    'codemirrorjs', 'libs/jsonlint',
    'shortcut'],
    function ($, Backbone, _, template,alerts) {
        var View = Backbone.View.extend({
            el:'#content',
            initialize:function () {
                this.isFullScreen= false;
                this.isPreview= false;
                this.Content= "";

                this.configuration=this.options.configuration;
                _.bindAll(this, "verify", "jumpToLine", "save","togglePreview","toggleFullScreen","switchToPreview");
                shortcut.add("F11", this.toggleFullScreen);
                shortcut.add("ctrl+p", this.togglePreview);
                shortcut.add("ctrl+f", this.verify);
                shortcut.add("ctrl+s", this.save);
            }, events:{
                "click #verify":"verify",
                "click #save":"save",
                "change #inputName":"change" ,
                "click #preview":"togglePreview",
                "click #fullScreen":"toggleFullScreen"
            },
            togglePreview:function () {
                if(this.isPreview){
                    this.codeMirror.setValue(this.Content);
                    this.codeMirror.setOption("readOnly",false);
                    this.isPreview=false;
                    $("#preview").removeClass("active");
                }
                else{
                    if (!this.verify())
                        return;
                    this.Content=this.codeMirror.getValue();
                    this.switchToPreview();
                }
            },
            switchToPreview:function(){
                var parentContent = (this.model.attributes.Parent!=null)
                    ?this.configuration.getBundle(this.model.attributes.Parent).attributes.Content
                    :{};
                var merged = $.extend({},jsonlint.parse(parentContent), jsonlint.parse(this.codeMirror.getValue()));
                this.codeMirror.setValue(JSON.stringify(merged, null, "  "));
                this.codeMirror.setOption("readOnly",true);
                this.isPreview=true;
                $("#preview").addClass("active");
            },
            toggleFullScreen: function () {
                var wrap = this.codeMirror.getWrapperElement();
                if (!this.isFullScreen) {
                    wrap.className += " CodeMirror-fullscreen";
                    $(".CodeMirror-fullscreen")
                        .height( window.innerHeight || (document.documentElement || document.body).clientHeight + "px")
                        .css("overflow","hidden");
                    document.documentElement.style.overflow = "hidden";
                    this.isFullScreen= true;
                    this.codeMirror.focus();
                } else {
                    wrap.className = wrap.className.replace(" CodeMirror-fullscreen", "");
                    $(".CodeMirror-fullscreen")
                        .height("")
                        .css("overflow","");
                    this.isFullScreen= false;
                }
                this.codeMirror.refresh();
            },
            'change':function(event){
                // Apply the change to the model
                var target = event.target;
                var change = {};
                if(event.target.type=="checkbox")
                    change[target.name] = target.value!="";
                else
                    change[target.name] = target.value;
                this.model.set(change);
            },
            save:function () {
                if(this.verify()){
                    var self = this;
                    var action=this.model.isNew()?"create":"update";
                    var content=this.isPreview?this.Content:this.codeMirror.getValue()
                    this.model.set("PureContent",content);
                    var options = {
                        success: function (model) {
                            self.Content=model.get("PureContent");
                            if(self.isPreview){
                                self.switchToPreview();
                            }
                            else
                                self.codeMirror.setValue(model.get("PureContent"));
                            alerts.show({type:"info",text:"Bundle '"+model.get("Name")+"' "+action+"d"});
                            self.navigate('#/configurations/'+model.get("Configuration")+"/bundles/"+model.id, true);
                        },
                        error: function (model,response) {
                            var bundleId=action==="update"?"'"+model.id+"'":"";
                            alerts.show({type:"error",text:"Failed to "+action+" bundle "+bundleId+". "+JSON.parse(response.responseText).Error});
                        },
                        wait:true
                    };

                    if(this.model.isNew())
                        this.configuration.createBundle(this.model,options);
                    else
                        this.model.save(null, options);
                }
            },
            verify:function () {
                try {
                    if (this.errorLine)
                        this.codeMirror.addLineClass(this.errorLine, null, null);
                    var result = jsonlint.parse(this.codeMirror.getValue());
                    this.codeMirror.setValue(JSON.stringify(result, null, "  "));
                    this.verificationErrors.text("").hide();
                    this.errorLine = undefined;
                    return true;
                } catch (ex) {
                    this.errorLine = (/Parse error on line (\d+)/g).exec(ex.message)[1] * 1 - 1;
                    this.verificationErrors.html("<strong>Error:</strong> " + ex.message).show();
                    this.codeMirror.addLineClass(this.errorLine, "error", "error");
                    this.jumpToLine(this.errorLine);
                    return false;
                }
            },
            jumpToLine:function (i) {
                this.codeMirror.setCursor(i);
                window.setTimeout(function () {
                    var line = $('.CodeMirror-lines .error');
                    var h = line.parent();
                    $('.CodeMirror-scroll').scrollTop(0).scrollTop(line.offset().top - $('.CodeMirror-scroll').offset().top - Math.round($('.CodeMirror-scroll').height() / 2));
                }, 200);
            },
            render:function () {
                this.template = _.template(template, { model:this.model.toJSON() });
                $(this.el).html(this.template);
                var self=this;
                this.codeMirror = CodeMirror.fromTextArea($(this.el).find("#code").get()[0], {
                    lineNumbers:true,
                    matchBrackets:true,
                    mode:"javascript",
                    lineNumberFormatter:function (integer) {
                        return integer + ".";
                    }
                });
                CodeMirror.on(window, "resize", function() {
                    var showing = $(".CodeMirror-fullscreen");
                    if (!showing) return;
                    showing.height( window.innerHeight || (document.documentElement || document.body).clientHeight + "px");
                });
                this.verificationErrors = $("#verificationErrors").hide();
                prettyPrint();
            },
            dispose:function(){
                shortcut.remove("F11");
                shortcut.remove("ctrl+p");
                shortcut.remove("ctrl+f");
                shortcut.remove("ctrl+s");
            }
        });

        return View;
    });
