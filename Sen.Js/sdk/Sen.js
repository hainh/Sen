this.Sen = {};
this.initSen = function(throwOnDataError) {
	Sen.throwOnDataError = throwOnDataError;
	internalInitSen(Sen);
}
function internalInitSen(Sen){

	/* Simple JavaScript Inheritance
	 * By John Resig http://ejohn.org/
	 * MIT Licensed.
	 */
	// Inspired by base2 and Prototype
	(function(){
	  var initializing = false, fnTest = /xyz/.test(function(){xyz;}) ? /\b_super\b/ : /.*/;
	 
	  // The base Class implementation (does nothing)
	  this.Class = function(){};
	 
	  // Create a new Class that inherits from this class
	  Class.extend = function(prop) {
		var _super = this.prototype;
	   
		// Instantiate a base class (but only create the instance,
		// don't run the init constructor)
		initializing = true;
		var prototype = new this();
		initializing = false;
	   
		// Copy the properties over onto the new prototype
		for (var name in prop) {
		  // Check if we're overwriting an existing function
		  prototype[name] = typeof prop[name] == "function" &&
			typeof _super[name] == "function" && fnTest.test(prop[name]) ?
			(function(name, fn){
			  return function() {
				var tmp = this._super;
			   
				// Add a new ._super() method that is the same method
				// but on the super-class
				this._super = _super[name];
			   
				// The method only need to be bound temporarily, so we
				// remove it when we're done executing
				var ret = fn.apply(this, arguments);        
				this._super = tmp;
			   
				return ret;
			  };
			})(name, prop[name]) :
			prop[name];
		}
	   
		// The dummy class constructor
		function Class() {
		  // All construction is actually done in the init method
		  if ( !initializing && this.init )
			this.init.apply(this, arguments);
		}
	   
		// Populate our constructed prototype object
		Class.prototype = prototype;
	   
		// Enforce the constructor to be what we expect
		Class.prototype.constructor = Class;
	 
		// And make this class extendable
		Class.extend = arguments.callee;
	   
		return Class;
	  };
	})();
	
	(function(Sen){
		var Status = Sen.Status = {
			CONNECTED: 0,

			// After all, this status indicates connection was disconnected
			DISCONNECTED: 1,

			// Client disconnects
			DISCONNECTED_BY_CLIENT: 2,

			// No network
			DISCONNECTED_NETWORK_FAILED: 3,

			// Timeout or connection disrupted
			DISCONNECTED_CONNECTION_LOST: 4,

			// Server refused to connect
			DISCONNECTED_SERVER_ABORTED: 5,

			// Internal socket error
			INTERNAL_ERROR: 6,

			// Socket address error
			SOCKET_ADDRESS_ERROR: 7,

			// Couldn't resolve address supplied
			RESOLVER_FAILED: 8,

			DISCONNECTED_UNKOWN_REASON: 2048,
		};

		/**
		* Peer to connect to server.
		* Set or override "onStatusChanged(statusCode)" to handle status changed event.
		* Set or override "onEvent(data, encrypted)" to handle events.
		* Set or override "onResponse(data, encrypted)" to handle responses.
		*/
		Sen.Client = Class.extend({
			init: function () {
				var this_ = this;
				this_.isConnecting = 0;
				this_.isConnected = 0;
				this_.onStatusChanged = function() {};
				this_.handlers = {};
			},
			connect: function (uri) {
				var this_ = this;

				if (this_.isConnecting || this_.isConnected) {
					return;
				}

				this_.isConnecting = 1;

				var socket = this_.socket = new WebSocket(uri);
				socket.binaryType = "arraybuffer";

				socket.onopen = (function(ev) {
					this.isConnected = 1;
					this.isConnecting = 0;
					this.onStatusChanged(Status.CONNECTED);
				}).bind(this);

				socket.onclose = (function (ev) {
					console.log(ev);
					this.onStatusChanged(Status.DISCONNECTED);
					this.isConnected = 0;
					this.isConnecting = 0;
				}).bind(this);

				socket.onerror = function(ev) {
					console.log(ev, ev.code);
				};

				socket.onmessage = this.__onMessage.bind(this);
			},
			disconnect: function() {
				var this_ = this;
				this_.onStatusChanged(Status.DISCONNECTED_BY_CLIENT);
				if (this_.isConnected) {
					this_.socket.close();
				}
			},
			send: function(data) {
				var rawData = Sen.Data.Serialize(data),
					this_ = this;
				if (this_.isConnected) {
					Sen.Logger.debug('Send', data.toString(this_.dataCodes, this_.opCodes));
					this_.socket.send(rawData);
				}
			},
			handleMessage: function(messageType, handler) {
				this.handlers[messageType] = handler;
			},
			__onMessage: function (message) {
				var rawData = new Uint8Array(message.data);
				var obj = MessagePack.decode(rawData);
				var wiredData = {};
				function setValue(msgpackData, destObject, MessageTypes) {
					var name = destObject.constructor.name;
					var typeDescObj = MessageTypes.__innerTypes__[name];
					var {values} = typeDescObj;

					for (var i = values.length - 1; i >= 0; i--) {
						var {valueName, keyCode, type, isArray} = values[i];
						if (MessageTypes[type]) {
							destObject[valueName] = isArray 
								? msgpackData.map(msgpackDataElement => setValue(msgpackDataElement, MessageTypes[type](), MessageTypes))
								: setValue(msgpackData[keyCode], MessageTypes[type](), MessageTypes);
						} else {
							destObject[valueName] = msgpackData[keyCode];
						}
					}
					return destObject;
				}
				wiredData.ServiceCode = obj[0];
				var unionCode = obj[1][0];
				var unionType = MessageTypes.__innerTypes__[unionCode + ''];
				wiredData.Data = setValue(obj[1][1], MessageTypes[unionType.className](), MessageTypes);
				return wiredData;
			}
		});

	})(Sen);

	(function processMessageTypes(MessageTypes) {
		var {types} = MessageTypes,
			typeDict = MessageTypes.__innerTypes__ = {},
			type;
		for (var i = types.length - 1; i >= 0; i--) {
			type = types[i];
			typeDict[type['className']] = typeDict[type['keyCode'] + ''] = type;
		}
	})(MessageTypes);
	MessageTypes.createType = function() {
		for (var className in this.__innerTypes__) {
			eval(`var ctor = function ${className}(){if(!new.target)return new ${className}();}`);
			var typeData = this.__innerTypes__[className];
			this[className] = ctor;
			for (var i = typeData['values'].length - 1; i >= 0; i--) {
				var {valueName, keyCode, type, isArray} = typeData['values'][i];
				Object.defineProperty(ctor.prototype, valueName, defineGetterAndSetter(valueName, type, isArray, /*Sen.Logger*/console));
			}
			ctor.prototype.toJSON = function() {
				var props = Object.getOwnPropertyDescriptors(this.constructor.prototype);
				var jsonObj = {};
				for (var propName in props) {
					if (typeof props[propName].get === 'function') {
						jsonObj[propName] = this[propName];
					}
				}
				return jsonObj;
			}
		}

		function defineGetterAndSetter(valueName, type, isArray, logger) {
			var minMax = {
				'Byte': [0, 0xFF],
				'SByte': [-0x7F, 0x7F],
				'Char': [0, 0xFFFF],
				'UInt16': [0, 0xFFFF],
				'Int16': [-0x7FFF, 0x7FFF],
				'UInt32': [0, 0xFFFFFFFF],
				'Int32': [-0x7FFFFFFF, 0x7FFFFFFF],
				'UInt64': [0, 0xFFFFFFFFFFFFFFFF],
				'Int64': [-0x7FFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF],
				'Decimal': 1,
				'Single' : 1,
				'Double' : 1,
			};
			function buildElementValidator(valueName, type) {
				var range = minMax[type];
				if (range === 1) {
					return `if(isNaN(v)) {${Sen.throwOnDataError ? 'throw ' : 'logger.error'}(\`\${type} value of \${this.constructor.name}.${valueName} = \${v} is not a number\`)}`
				} else if (range.length === 2) {
					return `if (isNaN(v) || v - Math.floor(v)) {
							${Sen.throwOnDataError ? 'throw ' : 'logger.error'}(\`${type} value of \${this.constructor.name}.${valueName} = \${v} is not an integer\`);
						}
						else if (v < ${range[0]} || v > ${range[1]}) {
							${Sen.throwOnDataError ? 'throw ' : 'logger.error'}(\`${type} value of \${this.constructor.name}.${valueName} = \${v} is out of range (${range[0]}, ${range[1]})\`);
						}`;
				}
				return '';
			}
			function buildValidator(valueName, type, isArray) {
				var elementValidator = buildElementValidator(valueName, type);
				if (elementValidator) {
					return isArray
						? 'for (var i = arr.length - 1; i >= 0; i--) \n{ var v = val[i];\n' + elementValidator + '\n}'
						: 'var v = val;\n' + elementValidator;
				}
				return '';
			}
			function buildSetter(valueName, type, isArray) {
				switch (type) {
					case 'Boolean': 
						return `function setter (val) { __rawData = ${isArray ? 'val.map(v => !!v)' : '!!val'};};`;
					case 'Byte':
					case 'SByte':
					case 'Char':
					case 'UInt16':
					case 'Int16':
					case 'UInt32':
					case 'Int32':
					case 'UInt64':
					case 'Int64':
					case 'Decimal':
					case 'Double':
					case 'Single':
						return `function setter (val){
							${buildValidator(valueName, type, isArray)}
							__rawData = ${isArray ? 'val.map(v => +v)' : '+val'};
						};`;
					case 'String':
						return `function setter (val) {
							__rawData = val && ${isArray ? 'val.map(v => v && v.toString())' : 'val.toString();'};
						};`;
					default:
						if (MessageTypes.__innerTypes__[type]) {
							return `function setter(val){
								if (!(val instanceof MessageTypes['${type}'])) {
									${Sen.throwOnDataError ? 'throw ' : 'logger.error'}(\`Value of \${this.constructor.name}.${valueName} = \${val} is not instanceof ${type}\`);
								}
								__rawData = val;
							};`
						} else {
							return `function setter(val) {__rawData = val;};`
						}
				}
			}
			var __rawData = null;
			var functionSetterScript = buildSetter(valueName, type, isArray);
			eval(functionSetterScript);
			var functionGetterScript = `function getter() {return __rawData};`;
			eval(functionGetterScript);
			return {
				get: getter,
				set: setter
			}
		}
	}
	MessageTypes.createType();
	delete MessageTypes.createType;

};

/*!
 * js-logger - http://github.com/jonnyreeves/js-logger
 * Jonny Reeves, http://jonnyreeves.co.uk/
 * js-logger may be freely distributed under the MIT license.
 */
(function (global) {
	"use strict";

	// Top level module for the global, static logger instance.
	var Logger = { };

	// For those that are at home that are keeping score.
	Logger.VERSION = "1.2.0";

	// Function which handles all incoming log messages.
	var logHandler;

	// Map of ContextualLogger instances by name; used by Logger.get() to return the same named instance.
	var contextualLoggersByNameMap = {};

	// Polyfill for ES5's Function.bind.
	var bind = function(scope, func) {
		return function() {
			return func.apply(scope, arguments);
		};
	};

	// Super exciting object merger-matron 9000 adding another 100 bytes to your download.
	var merge = function () {
		var args = arguments, target = args[0], key, i;
		for (i = 1; i < args.length; i++) {
			for (key in args[i]) {
				if (!(key in target) && args[i].hasOwnProperty(key)) {
					target[key] = args[i][key];
				}
			}
		}
		return target;
	};

	// Helper to define a logging level object; helps with optimisation.
	var defineLogLevel = function(value, name) {
		return { value: value, name: name };
	};

	// Predefined logging levels.
	Logger.DEBUG = defineLogLevel(1, 'DEBUG');
	Logger.INFO = defineLogLevel(2, 'INFO');
	Logger.TIME = defineLogLevel(3, 'TIME');
	Logger.WARN = defineLogLevel(4, 'WARN');
	Logger.ERROR = defineLogLevel(8, 'ERROR');
	Logger.OFF = defineLogLevel(99, 'OFF');

	// Inner class which performs the bulk of the work; ContextualLogger instances can be configured independently
	// of each other.
	var ContextualLogger = function(defaultContext) {
		this.context = defaultContext;
		this.setLevel(defaultContext.filterLevel);
		this.log = this.info;  // Convenience alias.
	};

	ContextualLogger.prototype = {
		// Changes the current logging level for the logging instance.
		setLevel: function (newLevel) {
			// Ensure the supplied Level object looks valid.
			if (newLevel && "value" in newLevel) {
				this.context.filterLevel = newLevel;
			}
		},

		// Is the logger configured to output messages at the supplied level?
		enabledFor: function (lvl) {
			var filterLevel = this.context.filterLevel;
			return lvl.value >= filterLevel.value;
		},

		debug: function () {
			this.invoke(Logger.DEBUG, arguments);
		},

		info: function () {
			this.invoke(Logger.INFO, arguments);
		},

		warn: function () {
			this.invoke(Logger.WARN, arguments);
		},

		error: function () {
			this.invoke(Logger.ERROR, arguments);
		},

		time: function (label) {
			if (typeof label === 'string' && label.length > 0) {
				this.invoke(Logger.TIME, [ label, 'start' ]);
			}
		},

		timeEnd: function (label) {
			if (typeof label === 'string' && label.length > 0) {
				this.invoke(Logger.TIME, [ label, 'end' ]);
			}
		},

		// Invokes the logger callback if it's not being filtered.
		invoke: function (level, msgArgs) {
			if (logHandler && this.enabledFor(level)) {
				logHandler(msgArgs, merge({ level: level }, this.context));
			}
		}
	};

	// Protected instance which all calls to the to level `Logger` module will be routed through.
	var globalLogger = new ContextualLogger({ filterLevel: Logger.OFF });

	// Configure the global Logger instance.
	(function() {
		// Shortcut for optimisers.
		var L = Logger;

		L.enabledFor = bind(globalLogger, globalLogger.enabledFor);
		L.debug = bind(globalLogger, globalLogger.debug);
		L.time = bind(globalLogger, globalLogger.time);
		L.timeEnd = bind(globalLogger, globalLogger.timeEnd);
		L.info = bind(globalLogger, globalLogger.info);
		L.warn = bind(globalLogger, globalLogger.warn);
		L.error = bind(globalLogger, globalLogger.error);

		// Don't forget the convenience alias!
		L.log = L.info;
	}());

	// Set the global logging handler.  The supplied function should expect two arguments, the first being an arguments
	// object with the supplied log messages and the second being a context object which contains a hash of stateful
	// parameters which the logging function can consume.
	Logger.setHandler = function (func) {
		logHandler = func;
	};

	// Sets the global logging filter level which applies to *all* previously registered, and future Logger instances.
	// (note that named loggers (retrieved via `Logger.get`) can be configured independently if required).
	Logger.setLevel = function(level) {
		// Set the globalLogger's level.
		globalLogger.setLevel(level);

		// Apply this level to all registered contextual loggers.
		for (var key in contextualLoggersByNameMap) {
			if (contextualLoggersByNameMap.hasOwnProperty(key)) {
				contextualLoggersByNameMap[key].setLevel(level);
			}
		}
	};

	// Retrieve a ContextualLogger instance.  Note that named loggers automatically inherit the global logger's level,
	// default context and log handler.
	Logger.get = function (name) {
		// All logger instances are cached so they can be configured ahead of use.
		return contextualLoggersByNameMap[name] ||
			(contextualLoggersByNameMap[name] = new ContextualLogger(merge({ name: name }, globalLogger.context)));
	};

	// Configure and example a Default implementation which writes to the `window.console` (if present).  The
	// `options` hash can be used to configure the default logLevel and provide a custom message formatter.
	Logger.useDefaults = function(options) {
		options = options || {};

		options.formatter = options.formatter || function defaultMessageFormatter(messages, context) {
			// Prepend the logger's name to the log message for easy identification.
			if (context.name) {
				messages.unshift("[" + context.name + "]");
			}
		};

		// Check for the presence of a logger.
		if (typeof console === "undefined") {
			return;
		}

		// Map of timestamps by timer labels used to track `#time` and `#timeEnd()` invocations in environments
		// that don't offer a native console method.
		var timerStartTimeByLabelMap = {};

		// Support for IE8+ (and other, slightly more sane environments)
		var invokeConsoleMethod = function (hdlr, messages) {
			Function.prototype.apply.call(hdlr, console, messages);
		};

		Logger.setLevel(options.defaultLevel || Logger.DEBUG);
		Logger.setHandler(function(messages, context) {
			// Convert arguments object to Array.
			messages = Array.prototype.slice.call(messages);

			var hdlr = console.log;
			var timerLabel;

			if (context.level === Logger.TIME) {
				timerLabel = (context.name ? '[' + context.name + '] ' : '') + messages[0];

				if (messages[1] === 'start') {
					if (console.time) {
						console.time(timerLabel);
					}
					else {
						timerStartTimeByLabelMap[timerLabel] = new Date().getTime();
					}
				}
				else {
					if (console.timeEnd) {
						console.timeEnd(timerLabel);
					}
					else {
						invokeConsoleMethod(hdlr, [ timerLabel + ': ' +
							(new Date().getTime() - timerStartTimeByLabelMap[timerLabel]) + 'ms' ]);
					}
				}
			}
			else {
				// Delegate through to custom warn/error loggers if present on the console.
				if (context.level === Logger.WARN && console.warn) {
					hdlr = console.warn;
				} else if (context.level === Logger.ERROR && console.error) {
					hdlr = console.error;
				} else if (context.level === Logger.INFO && console.info) {
					hdlr = console.info;
				}

				options.formatter(messages, context);
				invokeConsoleMethod(hdlr, messages);
			}
		});
	};

	// Export to popular environments boilerplate.
	if (typeof define === 'function' && define.amd) {
		define(Logger);
	}
	else if (typeof module !== 'undefined' && module.exports) {
		module.exports = Logger;
	}
	else {
		Logger._prevLogger = global.Logger;

		Logger.noConflict = function () {
			global.Logger = Logger._prevLogger;
			return Logger;
		};

		global.Logger = Logger;
	}
}(this));

Sen.Logger = Logger.get("Sen");
Logger.useDefaults({
	defaultLevel: Logger.DEBUG,
	formatter: function(message, context) {
		var now = new Date();
		var tag = "[" + ("0" + now.getHours()).substr(-2) + ":" +
				("0" + now.getMinutes()).substr(-2) + ":" + 
				("0" + now.getSeconds()).substr(-2) + "." + 
				("000" + now.getMilliseconds()).substr(-4);
		if (context.name) {
			tag = tag + " " + context.name;
		}
		tag = tag + "]";

		if (typeof window === "undefined") {
			var messageBulk = tag + " " + message.join("\r\n");
			message[0] = messageBulk;
			message.length = 1;
		} else {
			message.unshift(tag);
		}
	}
});