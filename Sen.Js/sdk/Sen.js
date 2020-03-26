Sen = {};
(function(Sen){

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
		Sen.PeerBase = Class.extend({
			init: function () {
				var this_ = this;
				this_.isConnecting = 0;
				this_.isConnected = 0;
				this_.onStatusChanged = this_.onEvent = this_.onResponse = function() {};
			},
			connect: function (uri) {
				var this_ = this;

				if (this_.isConnecting || this_.isConnected) {
					return;
				}

				this_.isConnecting = 1;

				var socket = this_.socket = new WebSocket(uri);
				socket.binaryType = "arraybuffer";

				socket.onopen = function(ev) {
					this_.isConnected = 1;
					this_.isConnecting = 0;
					this_.onStatusChanged(Status.CONNECTED);
				};

				socket.onclose = function (ev) {
					console.log(ev);
					this_.onStatusChanged(Status.DISCONNECTED);
					this_.isConnected = 0;
					this_.isConnecting = 0;
				};

				socket.onerror = function(ev) {
					console.log(ev, ev.code);
				};

				socket.onmessage = function(message) {
					var rawData = new Uint8Array(message.data);
					var data = Sen.Data.DeserializeData(rawData);
					var types = Sen.Data.ServiceTypes;
					switch (data["sc"]) {
						case types.EventData:
							this_.onEvent(data, false);
							break;
						case types.OperationData:
							this_.onResponse(data, false);
							break;
						case types.PingData:
							this_._sendPingData(data);
							break;
						case types.ConfigData:
							break;
						case types.EncryptData:
							// Decrypt message
							break;
					}
				};
			},
			disconnect: function() {
				var this_ = this;
				this_.onStatusChanged(Status.DISCONNECTED_BY_CLIENT);
				if (this_.isConnected) {
					this_.socket.close();
				}
			},
			sendOperatinRequest: function(opData) {
				if (opData["sc"] === Sen.Data.ServiceTypes.OperationData) {
					this._send(opData);
				}
			},
			setDebugCodeObjects: function(opCodes, evCodes, dataCodes) {
				var this_ = this;
				this_.opCodes = opCodes;
				this_.evCodes = evCodes;
				this_.dataCodes = dataCodes;
			},
			getPingTime: function() {
				return this.__pingTime;
			},
			_sendPingData: function(pingData) {
				this.__pingTime = pingData.get(1);
				this._send(pingData);
			},
			_send: function(data) {
				var rawData = Sen.Data.Serialize(data),
					this_ = this;
				if (this_.isConnected) {
					Sen.Logger.debug(data.toString(this_.dataCodes, this_.opCodes));
					this_.socket.send(rawData);
				}
			},
			_timeoutDisconnect: function() {
				var this_ = this;
				this_.onStatusChanged(Status.DISCONNECTED_CONNECTION_LOST);
				if (this_.isConnected) {
					this_.socket.close();
				}
			}
		});

	})(Sen);

	Sen.Data = {};

	(function(Data){
		function getKeyName(obj, value) {
			for (var key in obj) {
				if (obj[key] === value) {
					return "" + key;
				}
			}

			return "undefined";
		}
		Data.getKeyName = getKeyName;

		var ServiceTypes = Data.ServiceTypes = {
			EventData     : 0,
			OperationData : 1,
			PingData      : 2,
			EncryptData   : 3,
			ConfigData    : 4,
		};

		var DataTypes = Data.DataTypes = {
			BOOL : 0,
			BYTE : 1,
			INT16 : 2,
			INT32 : 3,
			INT64 : 4,
			UINT16 : 5,
			UINT32 : 6,
			UINT64 : 7,
			FLOAT : 8,
			DOUBLE : 9,
			STRING : 10,
			DICTIONARY : 11,

			ARRAY_BOOL : 16,
			ARRAY_BYTE : 17,
			ARRAY_INT16 : 18,
			ARRAY_INT32 : 19,
			ARRAY_INT64 : 20,
			ARRAY_UINT16 : 21,
			ARRAY_UINT32 : 22,
			ARRAY_UINT64 : 23,
			ARRAY_FLOAT : 24,
			ARRAY_DOUBLE : 25,
			ARRAY_STRING : 26,
			ARRAY : 16,// = ARRAY_BOOL,
		}

		var TypeStrings = [
			"BOOL",
			"BYTE",
			"INT16",
			"INT32",
			"INT64",
			"UINT16",
			"UINT32",
			"UINT64",
			"FLOAT",
			"DOUBLE",
			"STRING",
			,,,,,
			"ARRAY_BOOL",
			"ARRAY_BYTE",
			"ARRAY_INT16",
			"ARRAY_INT32",
			"ARRAY_INT64",
			"ARRAY_UINT16",
			"ARRAY_UINT32",
			"ARRAY_UINT64",
			"ARRAY_FLOAT",
			"ARRAY_DOUBLE",
			"ARRAY_STRING",
		]

		function createType(type) {
			return function(value) {
				var MinMaxValues = [ 
					, // bool
					{
						min: 0, max: 255
					} , //byte
					, // int16
					{
						min : -Math.pow(2, 31), max : Math.pow(2, 31) - 1
					}, //int32
					{
						min : -Math.pow(2, 53), max : Math.pow(2, 53) - 1
					}, //int64
					, // uint16
					{
						min : 0, max : Math.pow(2, 32) - 1
					}, //uint32
					{
						min : 0, max : Math.pow(2, 53) - 1
					}, //uint64
				];
				var minmax = MinMaxValues[type < DataTypes.ARRAY ? type : type - DataTypes.ARRAY];
				function inRange(element, index, array) {
					return element >= minmax.min && element <= minmax.max;
				}
				var needCheck = !!minmax;
				var typeName = TypeStrings[type];
				if (needCheck) {
					var all = type < DataTypes.ARRAY ? [value] : value;
					if (!all.every(inRange)) {
						Sen.Logger.error("value is out of range, type " + typeName + ", value " + all.join(", ") + "\r\nmin " + minmax.min + ", max " + minmax.max);
					}

					if (type < DataTypes.ARRAY)  {
						value = Math.floor(value);
					} else {
						for (var i in value) {
							value[i] = Math.floor(value[i]);
						}
					}
				}

				if (type === DataTypes.BOOL) {
					value = !!value;
				} else if (type === DataTypes.ARRAY_BOOL) {
					for (var i in value) {
						value[i] = !!value[i];
					}
				}
				var valueWrapper = {
					value : value,
					type : type,
					typeName : typeName
				};

				return valueWrapper;
			}
		}

		Sen.Bool = createType(DataTypes.BOOL);
		Sen.Byte = createType(DataTypes.BYTE);
		Sen.Int32 = createType(DataTypes.INT32);
		Sen.UInt32 = createType(DataTypes.UINT32);
		Sen.Int64 = createType(DataTypes.INT64);
		Sen.UInt64 = createType(DataTypes.UINT64);
		Sen.Float = createType(DataTypes.FLOAT);
		Sen.Double = createType(DataTypes.DOUBLE);
		Sen.String = createType(DataTypes.STRING);
		
		Sen.BoolArray = createType(DataTypes.ARRAY_BOOL);
		Sen.ByteArray = createType(DataTypes.ARRAY_BYTE);
		Sen.Int32Array = createType(DataTypes.ARRAY_INT32);
		Sen.UInt32Array = createType(DataTypes.ARRAY_UINT32);
		Sen.Int64Array = createType(DataTypes.ARRAY_INT64);
		Sen.UInt64Array = createType(DataTypes.ARRAY_UINT64);
		Sen.FloatArray = createType(DataTypes.ARRAY_FLOAT);
		Sen.DoubleArray = createType(DataTypes.ARRAY_DOUBLE);
		Sen.StringArray = createType(DataTypes.ARRAY_STRING);

		(function(Data){
			var DataContainer = Class.extend({
				init: function(code, parameters) {
					this.Code = code || 0;
					this.Parameters = parameters;
				},
				add: function(key, value) {
					this.Parameters[key] = value;
				},
				get: function(key) {
					return this.Parameters[key].value;
				},
				asEventData: function() {
					this.__serviceCode = ServiceTypes.EventData;
					return this;
				},
				asOperationData: function() {
					this.__serviceCode = ServiceTypes.OperationData;
					return this;
				},
				asPing: function() {
					this.__serviceCode = ServiceTypes.PingData;
					return this;
				},
				asConfigData: function() {
					this.__serviceCode = ServiceTypes.ConfigData;
					return this;
				},
				toString: function(keysObj, codesObj) {
					var _this = this;
					var code = _this.Code;
					var params = _this.Parameters;
					var hasOwnProperty = Object.getOwnPropertyNames(params || {}).length;
					var string = _this.__serviceCode + getKeyName(ServiceTypes, _this.__serviceCode) + " code " + code
								+ (!codesObj ? "" : (" - " + getKeyName(codesObj, code)))
								+ (hasOwnProperty ? " {\r\n" : "");

					if (hasOwnProperty) {
						var maxTypeStringLength = 0, maxKeyStringLength = 0, typeLength;
						for (var key in params) {
							typeLength = getKeyName(DataTypes, params[key].type).length;
							maxTypeStringLength = maxTypeStringLength < typeLength ? typeLength : maxTypeStringLength;
						}
						maxTypeStringLength += 4;
						if (keysObj) {
							var keysInParams = Object.getOwnPropertyNames(params);
							for (var keyName in keysObj) {
								if (keysInParams.indexOf("" + keysObj[keyName]) >= 0) {
									maxKeyStringLength = Math.max(maxKeyStringLength, keyName.length);
								}
							}
							maxKeyStringLength += 2;
						}
						for (var key in params) {
							var param = params[key];
							string += "  " + (keysObj ? "<" + getKeyName(keysObj, parseInt(key, 10)) + ">                   " : "").substr(0, maxKeyStringLength) + ("  " + key).substr(-3) + 
									(":<" + getKeyName(DataTypes, param.type) + ">          ").substr(0, maxTypeStringLength) + 
									(param.type < DataTypes.ARRAY ? param.value : "[" + param.value.join(", ") + "]") + "\r\n";
						}
						string += "}";
					}
					return string;
				}
			});

			/* serviceCode getter
			*/
			Object.defineProperty(DataContainer.prototype, "sc", {
				get: function() {
					return this.__serviceCode;
				}
			});
			
			Data.DataContainer = DataContainer;
		})(Data);

		(function(Serializer) {
			/// <summary>
			/// Encode integer types of 1 to 8-byte length.
			/// </summary>
			/// <param name="value"></param>
			/// <param name="output"></param>
			/// <returns></returns>
			/*static List<byte>*/
			function encodeVarUInt(/*ulong*/ value, /*List<byte>*/ output)
			{
				output.length = 0;

				var byte_ = 0;
				var maxUint32 = 0x7FFFFFFF;
				do {
					if (value <= maxUint32) {
						byte_ = value & 0x7F;
						value = value >> 7;
					} else {
						// TODO: Need more optimization
						byte_ = value % 0x80; // value & 0x7F
						value = Math.floor(value / 0x80); //value >> 7
					}
					if (value > 0) {
						byte_ = byte_ | 0x80; // indicate that this byte has a follower
					}

					output.push(byte_);
				} while (value > 0);

				return output;
			}

			/// <summary>
			/// Encode numberic type that equal or more than 2 bytes length as a "jagged" value
			/// </summary>
			/// <param name="originValue"></param>
			/// <param name="output"></param>
			/// <returns></returns>
			/*static List<byte>*/
			function encodeVarInt(/*long*/ originValue, /*List<byte>*/ output)
			{
				// To "jagged" value
				var value = getJaggedValue(originValue);
				return encodeVarUInt(value, output);
			}

			function lshift5(value) {
				return value <= 0x3ffffff ? value << 5 : value * 32; 
			}

			/*static ulong */
			function getJaggedValue(/*long */originValue)
			{
				var maxUint32 = 0x3FFFFFFF;
				if (Math.abs(originValue) < maxUint32)
					return originValue < 0 ? ((-originValue) << 1) | 0x01 : originValue << 1;
				else
					return originValue < 0 ? (-originValue * 2 + 1) : originValue * 2;
			}

			function pushToThisArray(entry){
				this.push(entry);
			}
			Array.prototype.pushAll = function(another) {
				another.forEach(pushToThisArray, this);
			}

			function unshiftAllToThisArray(entry) {
				this.unshift(entry);
			}
			Array.prototype.unshiftAll = function(another) {
				another.reverse().forEach(unshiftAllToThisArray, this);
			}

			function toUtf8(string) {
				var utf8 = unescape(encodeURIComponent(string));

				var arr = [];
				for (var i = 0; i < utf8.length; i++) {
					arr.push(utf8.charCodeAt(i));
				}
				return arr;
			}

			function floatToArray(floatNum) {
				var ab = new ArrayBuffer(4);
				var fa = new Float32Array(ab);
				var u8 = new Uint8Array(ab);
				fa[0] = floatNum;
				return u8;
			}

			function doubleToArray(doubleNum) {
				var ab = new ArrayBuffer(8);
				var fa = new Float64Array(ab);
				var u8 = new Uint8Array(ab);
				fa[0] = doubleNum;
				return u8;
			}

			function Serialize(data){
				
				var buffer = [];
				var encodedBuffer = [];

					// Put data code
				buffer.push(data.Code);

				if (data.Parameters == null)
				{
					buffer.unshift(data["sc"] | (1 << 3)); // 1 == buffer.length

					// return the buffer array
					return buffer;
				}

				parameters = data.Parameters;
				for (var key in parameters)
				{
					var keyAsNumber = parseInt(key, 10);
					if (keyAsNumber < 0 || keyAsNumber > 255) {
						Sen.Logger.error("Parameters' key must be a byte number: key = " + key);
					}

					// Put parameter code
					buffer.push(keyAsNumber);
					var param = parameters[key];
					var type = param.type;
					var value = param.value;

					if (type >= DataTypes.ARRAY){
						if (!value || value.length === 0) {
							buffer.push(DataTypes.ARRAY);
						} else if (type == DataTypes.ARRAY_BYTE) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							buffer.pushAll(value);
						} else if (type === DataTypes.ARRAY_BOOL) {
							// Pack 8 values of boolean in 1 byte
							var bools = value;
							var length = bools.length;
							param = type | (length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));

							var bytesNeeded = length / 8 + (length % 8 > 0 ? 1 : 0);
							var b = 0;
							for (var i = 0; i < length; i++)
							{
								if (bools[i])
								{
									b = b | (1 << (i % 8));
								}

								if ((i + 1) % 8 == 0)
								{
									buffer.push(b);
									b = 0;
								}
								else if (i == length - 1)
								{
									buffer.push(b);
								}
							}
						} else if (type === DataTypes.ARRAY_INT32 || type === DataTypes.ARRAY_INT64) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							for (var i = 0; i < value.length; i++)
							{
								buffer.pushAll(encodeVarInt(value[i], encodedBuffer));
							}
						} else if (type === DataTypes.ARRAY_UINT32 || type === DataTypes.ARRAY_UINT64) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							for (var i = 0; i < value.length; i++)
							{
								buffer.pushAll(encodeVarUInt(value[i], encodedBuffer));
							}
						}else if (type === DataTypes.ARRAY_FLOAT) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							for (var i = 0; i < value.length; i++)
							{
								buffer.pushAll(floatToArray(value[i]));
							}
						} else if (type === DataTypes.ARRAY_DOUBLE) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							for (var i = 0; i < value.length; i++)
							{
								buffer.pushAll(doubleToArray(value[i]));
							}
						} else if (type === DataTypes.ARRAY_STRING) {
							param = type | (value.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							var s;
							for (var i = 0; i < value.length; i++)
							{
								s = value[i] || "";
								var bytes = toUtf8(s);
								buffer.pushAll(encodeVarUInt(bytes.length, encodedBuffer));
								buffer.pushAll(bytes);
							}
						} else {
							Sen.Logger.warn("Not supported type " + type);
						}
					}
					else {
						if (type === DataTypes.BOOL) {
							param = type | (!!value ? (1 << 5) : 0);
							buffer.push(param);
						} else if (type === DataTypes.BYTE) {
							param = type | (value << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
						} else if (type === DataTypes.INT32) {
							param = type + lshift5(getJaggedValue(value)); // special case where "type < (1 << 5)" then we use plus+ instead of or| to fix bug numberic overflow of javascript
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
						} else if (type === DataTypes.INT64){
							buffer.push(type)
							param = getJaggedValue(value);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
						} else if (type === DataTypes.UINT32) {
							param = type + lshift5(value); // special case where "type < (1 << 5)" then we use plus+ instead of or|
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
						} else if (type === DataTypes.UINT64) {
							buffer.push(type)
							buffer.pushAll(encodeVarUInt(value, encodedBuffer));
						} else if (type === DataTypes.FLOAT) {
							buffer.push(type);
							buffer.pushAll(floatToArray(value));
						} else if (type === DataTypes.DOUBLE) {
							buffer.push(type);
							buffer.pushAll(doubleToArray(value));
						} else if (type === DataTypes.STRING) {
							var bytes = toUtf8(value || "");
							param = type | (bytes.length << 5);
							buffer.pushAll(encodeVarUInt(param, encodedBuffer));
							buffer.pushAll(bytes);
						} else {
							Sen.Logger.warn("Not supported type " + type);
						}
					}
				}

				var size = buffer.length;

				param = (size << 3) | data["sc"];
				encodeVarUInt(param, encodedBuffer);
				buffer.unshiftAll(encodedBuffer);

				var ui8Array = new Uint8Array(buffer.length);
				buffer.forEach(copyToThisUint8Array, ui8Array);
				// return the buffer array
				return ui8Array;
			}

			function copyToThisUint8Array(entry, index) {
				this[index] = entry;
			}

			Serializer.Serialize = Serialize;
		})(Data);

		(function(Deserializer) {
			// return number << (7 * i)
			function lshift7xi(number, i) {
				return i === 0 ? number : (i === 1 ? number : lshift7xi(number, i - 1)) * 128;
			}

			function TryReadVarInt(data, out_varInt, ref_offset)
			{
				out_varInt.value = 0;
				if (data.length == 0)
				{
					return false;
				}
				
				var i = 0;
				var shift;
				while(true)
				{
					var b = data[ref_offset.value];
					shift = 7 * i;
					out_varInt.value = i < 4 ? out_varInt.value | ((b & 0x7F) << shift)
											 : out_varInt.value + lshift7xi(b & 0x7F, i);
					++ref_offset.value;
					++i;
					if (shift > 63)
					{
						Sen.Logger.error("VarInt is overflow");
						return false;
					}
					if (b < 0x80)
					{
						return true;
					}

					if (ref_offset.value >= data.length)
					{
						ref_offset.value -= i; // reset offset
						return false;
					}
				}

			}

			function ReadByte(data, ref_offset)
			{
				return data[ref_offset.value++];
			}

			// function Discard(data, count)
			// {
			//     for (var i = 0; i < count; i++)
			//     {
			//         data.shift();
			//     }
			// }

			function ToSignedInt(num)
			{
				var sign = (num % 2) ? -1 : 1;

				return sign * (num < 0x7fffffff ? num >> 1 : Math.floor(num / 2));
			}

			var bits = new Uint8Array(16);

			function ReadFloat(data, ref_offset)
			{
				bits[0] = data[ref_offset.value++];
				bits[1] = data[ref_offset.value++];
				bits[2] = data[ref_offset.value++];
				bits[3] = data[ref_offset.value++];

				var float32 = new Float32Array(bits.buffer);
				return float32[0];
			}

			function ReadDouble(data, ref_offset)
			{
				bits[0] = data[ref_offset.value++];
				bits[1] = data[ref_offset.value++];
				bits[2] = data[ref_offset.value++];
				bits[3] = data[ref_offset.value++];
				bits[4] = data[ref_offset.value++];
				bits[5] = data[ref_offset.value++];
				bits[6] = data[ref_offset.value++];
				bits[7] = data[ref_offset.value++];


				var float64 = new Float64Array(bits.buffer);

				return float64[0];
			}

			function Utf8ArrayToStr(array) {
				var out, i, len, c;
				var char2, char3;

				out = "";
				len = array.length;
				i = 0;
				while(i < len) {
					c = array[i++];
					switch(c >> 4)
					{ 
					  case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
						// 0xxxxxxx
						out += String.fromCharCode(c);
						break;
					  case 12: case 13:
						// 110x xxxx   10xx xxxx
						char2 = array[i++];
						out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
						break;
					  case 14:
						// 1110 xxxx  10xx xxxx  10xx xxxx
						char2 = array[i++];
						char3 = array[i++];
						out += String.fromCharCode(((c & 0x0F) << 12) |
									   ((char2 & 0x3F) << 6) |
									   ((char3 & 0x3F) << 0));
						break;
					}
				}

				return out;
			}

			function ReadString(data, ref_offset, length)
			{
				if (length == 0)
				{
					return "";
				}

				var bytes = data.slice(ref_offset.value, ref_offset.value + length);
				ref_offset.value += length;

				return Utf8ArrayToStr(bytes);
			}

			function ReadBoolArray(data, ref_offset, length)
			{
				// Special case that carries a null
				if (length == 0)
				{
					return null;
				}

				// var bytesNeeded = Math.floor(length / 8) + (length % 8 > 0 ? 1 : 0);
				var bools = [];
				bools.length = length;

				for (var i = 0; i < length; i++)
				{
					bools[i] = (data[ref_offset.value] & (1 << (i % 8))) > 0;
					if ((i + 1) % 8 == 0)
					{
						++ref_offset.value;
					}
					else if (i == length - 1)
					{
						++ref_offset.value;
					}
				}

				return bools;
			}

			function ReadByteArray(data, ref_offset, length)
			{
				var bytes = data.slice(ref_offset.value, ref_offset.value + length);
				ref_offset.value += length;

				return bytes;
			}

			function ReadInt16Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = ToSignedInt(value.value);
				}

				return ss;
			}

			function ReadInt32Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = ToSignedInt(value.value);
				}

				return ss;
			}

			function ReadInt64Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = ToSignedInt(value.value);
				}

				return ss;
			}

			function ReadUint16Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = value.value;
				}

				return ss;
			}

			function ReadUint32Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = value.value;
				}

				return ss;
			}

			function ReadUint64Array(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var value = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, value, ref_offset);
					ss[i] = value.value;
				}

				return ss;
			}

			function ReadFloatArray(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				for (var i = 0; i < length; i++)
				{
					ss[i] = ReadFloat(data, ref_offset);
				}

				return ss;
			}

			function ReadDoubleArray(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				for (var i = 0; i < length; i++)
				{
					ss[i] = ReadDouble(data, ref_offset);
				}

				return ss;
			}

			function ReadStringArray(data, ref_offset, length)
			{
				var ss = [];
				ss.length = length;
				var slen = {};
				for (var i = 0; i < length; i++)
				{
					TryReadVarInt(data, slen, ref_offset);
					ss[i] = ReadString(data, ref_offset, slen.value);
				}

				return ss;
			}

			function rshift5(value) {
				return value < 0x7fffffff ? value >> 5 : Math.floor(value / 32);
			}

			function ReadParameters(data, ref_offset, maxOffset)
			{
				var parameters = {};
				var varInt = {};
				var code;
				var type;
				var value;

				while (ref_offset.value < maxOffset)
				{
					code = ReadByte(data, ref_offset);
					TryReadVarInt(data, varInt, ref_offset);

					type = varInt.value % 0x20; // 5 bits type
					value = rshift5(varInt.value);
					var length = value;

					switch (type)
					{
						case DataTypes.BOOL:
							parameters[code] = Sen.Bool(value);
							break;
						case DataTypes.BYTE:
							parameters[code] = Sen.Byte(value);
							break;
						case DataTypes.INT16:
							parameters[code] = Sen.Int32(ToSignedInt(value));
							break;
						case DataTypes.INT32:
							parameters[code] = Sen.Int32(ToSignedInt(value));
							break;
						case DataTypes.INT64:
							TryReadVarInt(data, varInt, ref_offset);
							parameters[code] = Sen.Int64(ToSignedInt(varInt.value));
							break;
						case DataTypes.UINT16:
							parameters[code] = Sen.UInt32(value);
							break;
						case DataTypes.UINT32:
							parameters[code] = Sen.UInt32(value);
							break;
						case DataTypes.UINT64:
							TryReadVarInt(data, varInt, ref_offset);
							parameters[code] = Sen.UInt64(varInt.value);
							break;
						case DataTypes.FLOAT:
							parameters[code] = Sen.Float(ReadFloat(data, ref_offset));
							break;
						case DataTypes.DOUBLE:
							parameters[code] = Sen.Double(ReadDouble(data, ref_offset));
							break;
						case DataTypes.STRING:
							var str = ReadString(data, ref_offset, value);
							parameters[code] = Sen.String(str);
							break;
						case DataTypes.DICTIONARY:
							break;
						case DataTypes.ARRAY_BOOL: // Special case that 'bools = null'
							var bools = ReadBoolArray(data, ref_offset, length);
							parameters[code] = Sen.BoolArray(bools);
							break;
						case DataTypes.ARRAY_BYTE:
							var bytes = ReadByteArray(data, ref_offset, length);
							parameters[code] = Sen.ByteArray(bytes);
							break;
						case DataTypes.ARRAY_INT16:
							var ss = ReadInt16Array(data, ref_offset, length);
							parameters[code] = Sen.Int32Array(ss);
							break;
						case DataTypes.ARRAY_INT32:
							var iis = ReadInt32Array(data, ref_offset, length);
							parameters[code] = Sen.Int32Array(iis);
							break;
						case DataTypes.ARRAY_INT64:
							var ls = ReadInt64Array(data, ref_offset, length);
							parameters[code] = Sen.Int64Array(ls);
							break;
						case DataTypes.ARRAY_UINT16:
							var us = ReadUint16Array(data, ref_offset, length);
							parameters[code] = Sen.UInt32Array(us);
							break;
						case DataTypes.ARRAY_UINT32:
							var uis = ReadUint32Array(data, ref_offset, length);
							parameters[code] = Sen.UInt32Array(uis);
							break;
						case DataTypes.ARRAY_UINT64:
							var uls = ReadUint64Array(data, ref_offset, length);
							parameters[code] = Sen.UInt64Array(uls);
							break;
						case DataTypes.ARRAY_FLOAT:
							var fs = ReadFloatArray(data, ref_offset, length);
							parameters[code] = Sen.FloatArray(fs);
							break;
						case DataTypes.ARRAY_DOUBLE:
							var ds = ReadDoubleArray(data, ref_offset, length);
							parameters[code] = Sen.DoubleArray(ds);
							break;
						case DataTypes.ARRAY_STRING:
							var strs = ReadStringArray(data, ref_offset, length);
							parameters[code] = Sen.StringArray(strs);
							break;
						default:
							break;
					}
				}

				if (ref_offset.value != maxOffset)
				{
					throw "Deserialized frame too large " + ref_offset.value + " > " + maxOffset;
				}

				return parameters;
			}

			function DeserializeData(rawData)
			{
				var varInt = {};
				var offset = {value : 0};

				if (!TryReadVarInt(rawData, varInt, offset))
				{
					return null;
				}

				//ServiceType
				var type = varInt.value & 0x07;
				var frameSize = varInt.value >> 3;

				var maxSize = 65535; // 64kB
				if (frameSize > maxSize)
				{
					throw "Frame size too large";
				}

				// Not enough data for a frame
				if (rawData.length - offset.value < frameSize)
				{
					return null;
				}

				var code = ReadByte(rawData, offset);
				var parameters = ReadParameters(rawData, offset, frameSize + offset.value - 1);
				var dataContainer = new Data.DataContainer(code, parameters);
				switch (type)
				{
					case ServiceTypes.EventData:
						dataContainer.asEventData();
						break;
					case ServiceTypes.OperationData:
						dataContainer.asOperationData();
						break;
					case ServiceTypes.PingData:
						dataContainer.asPing();
						break;
					case ServiceTypes.EncryptData:
						break;
					case ServiceTypes.ConfigData:
						dataContainer.asConfigData();
						break;
					default:
						break;
				}

				return dataContainer;
			}

			Deserializer.DeserializeData = DeserializeData;
		})(Data);

	})(Sen.Data);

})(Sen);

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