this.Sen = {};
this.initSen = function(throwOnDataError) {
	if (typeof MessageTypes === 'undefined') {
		throw 'MessageTypes is missing. Generate one in Server project.'
	}
	Sen.throwOnDataError = throwOnDataError;
	__internalInitSen(Sen);
}
function __internalInitSen(Sen){

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
		try {
			var newClassName = prop.$className.split(/\W/)[0];
		} catch {
			throw 'Extend class must have "$className" property.';
		}
		delete prop.$className;

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
		
		var e = `// The dummy class constructor
		var ctor = function ${newClassName}() {
			// All construction is actually done in the init method
			if ( !initializing && this.init )
				this.init.apply(this, arguments);
		}`;
		eval(e);

		// Populate our constructed prototype object
		ctor.prototype = prototype;

		// Enforce the constructor to be what we expect
		ctor.prototype.constructor = ctor;

		// And make this class extendable
		ctor.extend = arguments.callee;

		return ctor;
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
		* Call handleMessage(messageType, handler) to register message handler.
		*/
		Sen.Client = Class.extend({
			$className: 'Client',
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

				socket.onopen = (function onopen(ev, ex) {
					Sen.Logger.debug('Connected', ev, ex);
					this.isConnected = 1;
					this.isConnecting = 0;
					this.onStatusChanged(Status.CONNECTED);
				}).bind(this);

				socket.onclose = (function onclose(ev) {
					Sen.Logger.debug('Disconnected with code', ev.code, ev);
					this.onStatusChanged(Status.DISCONNECTED);
					this.isConnected = 0;
					this.isConnecting = 0;
				}).bind(this);

				socket.onerror = function onerror(ev) {
					Sen.Logger.debug('WebSocket Error', ev);
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
			send: function(message) {
				if (this.isConnected) {
					var buffer = serialize(message);
					if (Sen.Logger.enabledFor(SLogger.DEBUG)) {
						Sen.Logger.debug('Sending message type:', message.constructor.name, message.toString());
					}
					this.socket.send(buffer);
				} else {
					Sen.Logger.debug('Cannot send data. Connection closed.');
				}
			},
			handleMessage: function(messageType, handler) {
				this.handlers[messageType] = handler;
			},
			__onMessage: function (wsMessage) {
				var buffer = new Uint8Array(wsMessage.data);
				var message = deserialize(buffer);
				var handler = this.handlers[message.constructor.name];
				if (Sen.Logger.enabledFor(SLogger.DEBUG)) {
					Sen.Logger.debug('Incoming message'  + (handler ? ' type' : ' handler missing'), ':', message.constructor.name, message.toString());
				}
				handler && handler(message);
			}
		});

	})(Sen);

	(function processMessageTypes(MessageTypes) {
		var {types} = MessageTypes,
			typeDict = MessageTypes.__innerTypes__ = {},
			typeCodeDict = MessageTypes.__innerTypesByCode__ = {},
			type;
		for (var i = types.length - 1; i >= 0; i--) {
			type = types[i];
			typeDict[type['className']] = type;
			if (type['unionCode'] >= 0) {
				typeCodeDict[type['unionCode'] + ''] = type;
			}
			type.maxTypeLength = Math.max.apply(Math, type.values.map(v => v.type.length + (v.isArray ? 3 : 1)));
			if (!type.values.find(value => value.type === 'Double')) {
				type.forceFloat32 = true;
			}
		}
		for (var i = types.length - 1; i >= 0; i--) {
			type = types[i];
			if (!type.values.find(value => value.type === 'Double' || (typeDict[value.type] && !typeDict[value.type].forceFloat32))) {
				type.forceFloat32 = true;
			}
		}
	})(MessageTypes);
	MessageTypes.createType = function() {
		for (var className in this.__innerTypes__) {
			eval(`var ctor = function ${className}(){if(!new.target)return new ${className}();}`);
			var typeData = this.__innerTypes__[className];
			this[className] = ctor;
			for (var i = 0; i < typeData['values'].length; i++) {
				var {valueName, keyCode, type, isArray} = typeData['values'][i];
				Object.defineProperty(ctor.prototype, valueName, defineGetterAndSetter(valueName, type, isArray, Sen.Logger));
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
			ctor.prototype.toString = function() {
				var obj = this.toJSON(),
					{values, maxTypeLength} = MessageTypes.__innerTypes__[this.constructor.name],
					cloneObj = {},
					getName = (value, maxTypeLength) => {
						var spaces = [],
							name = value.type + (value.isArray ? '[]' : '');
						spaces.length = maxTypeLength - name.length;
						spaces.fill(' ');
						return name + spaces.join('');
					};
				for (var key in obj) {
					var value = values.find(v => v.valueName === key);
					if (value) {
						cloneObj[getName(value, maxTypeLength) + key] = obj[key];
					} else {
						cloneObj[key] = obj[key];
					}
				}
				var json = JSON.stringify(cloneObj, null, '\t');
				var matches = json.match(/".*":/gm);
				for (var i = matches.length - 1; i >= 0; i--) {
					var key = matches[i],
						newKey = key.replace(/"/g, '');
					json = json.replace(key, newKey);
				}
				return json;
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
						? 'if (val) for (var i = val.length - 1; i >= 0; i--) \n{ var v = val[i];\n' + elementValidator + '\n}'
						: 'var v = val;\n' + elementValidator;
				}
				return '';
			}
			function buildSetter(valueName, type, isArray) {
				switch (type) {
					case 'Boolean':
						__rawData = false;
						return `function setter (val) { __rawData = ${isArray ? 'val && val.map(v => !!v)' : '!!val'};};`;
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
						__rawData = 0;
						return `function setter (val){
							${buildValidator(valueName, type, isArray)}
							__rawData = ${isArray ? 'val && val.map(v => +v)' : '+val'};
						};`;
					case 'String':
						return `function setter (val) {
							__rawData = val && ${isArray ? 'val.map(v => v && v.toString())' : 'val.toString();'};
						};`;
					default:
						if (MessageTypes.__innerTypes__[type]) {
							return `function setter(val){
								if (val != null && val != undefined && ${isArray ? `val.find(v => !(v instanceof MessageTypes.${type}))` : `!(val instanceof MessageTypes.${type})`}) {
									${Sen.throwOnDataError ? 'throw ' : 'logger.error'}(\`Value of \${this.constructor.name}.${valueName} = \${JSON.stringify(val)} (\${val.constructor.name}) is not instanceof ${type}\`);
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

	/*Serialize a message appended to full wired data to byte buffer*/
	function serialize (message) {
		var dataType = message.constructor.name;
		var typeDesc = MessageTypes.__innerTypes__[dataType];
		if (!typeDesc || typeDesc.unionCode < 0) {
			throw `Type MessageTypes.${dataType} is not supported`;
		}
		var msgpackData = [0, [typeDesc.unionCode, []]];
		getValues(message, msgpackData[1][1], MessageTypes);
		return MessagePack.encode(msgpackData, {forceFloat32: typeDesc.forceFloat32});

		function getValues(message, msgpackData, MessageTypes) {
			var {values} = MessageTypes.__innerTypes__[message.constructor.name];
			for (var i = values.length - 1; i >= 0; i--) {
				var {valueName, keyCode, type, isArray} = values[i];
				var value = message[valueName];
				if (MessageTypes[type]) {
					msgpackData[keyCode] = isArray 
						? value.map(message => getValues(message, [], MessageTypes))
						: getValues(value, [], MessageTypes);
				} else {
					msgpackData[keyCode] = value;
				}
			}
			return msgpackData;
		}
	}

	/*Deserialize a byte buffer to a message object*/
	function deserialize (buffer) {
		var obj = MessagePack.decode(buffer);
		var unionCode = obj[1][0];
		var unionType = MessageTypes.__innerTypesByCode__[unionCode + ''];
		return setValue(obj[1][1], MessageTypes[unionType.className](), MessageTypes);

		function setValue(msgpackData, destObject, MessageTypes) {
			var name = destObject.constructor.name;
			var {values} = MessageTypes.__innerTypes__[name];

			for (var i = values.length - 1; i >= 0; i--) {
				var {valueName, keyCode, type, isArray} = values[i],
					buffer = msgpackData[keyCode];
				if (MessageTypes[type]) {
					destObject[valueName] = isArray 
						? buffer.map(msgpackDataElement => setValue(msgpackDataElement, MessageTypes[type](), MessageTypes))
						: setValue(buffer, MessageTypes[type](), MessageTypes);
				} else {
					destObject[valueName] = buffer;
				}
			}
			return destObject;
		}
	}
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
		this.trace = false;
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

		setTrace: function(enable) {
			this.trace = !!enable;
		},

		// Invokes the logger callback if it's not being filtered.
		invoke: function (level, msgArgs) {
			if (logHandler && this.enabledFor(level)) {
				logHandler(msgArgs, merge({ level: level, trace: this.trace }, this.context));
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
			global.SLogger = Logger._prevLogger;
			return Logger;
		};

		global.SLogger = Logger;
	}
}(this));

Sen.Logger = SLogger.get("Sen");
SLogger.useDefaults({
	defaultLevel: SLogger.DEBUG,
	formatter: function(message, context) {
		var now = new Date();
		var tag = "[" + ("0" + now.getHours()).substr(-2) + ":" +
				("0" + now.getMinutes()).substr(-2) + ":" + 
				("0" + now.getSeconds()).substr(-2) + "." + 
				("000" + now.getMilliseconds()).substr(-4);
		if (context.name) {
			tag += " " + context.name;
		}

		tag += " " + context.level.name + "]";

		if (typeof window === "undefined") {
			var messageBulk = tag + " " + message.join("\r\n");
			message[0] = messageBulk;
			message.length = 1;
		} else {
			message.unshift(tag);
			if (context.trace && Error) {
				var stack = new Error().stack;
				if (stack) {
					message.push('\n');
					stack = stack.split('\n').slice(5);
					stack.unshift('Stack');
					message.push(stack.join('\n'));
				}
			}
		}
	}
});
Sen.Logger.setTrace(true);
// Sen.Logger.setLevel(SLogger.ERROR)