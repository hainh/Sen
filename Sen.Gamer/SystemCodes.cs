#if C_SHARP

namespace Senla.Gamer
{
    public enum OperationCode : byte
#else
    enum class OperationCode
#endif
    {
        LOGIN                   = 245,
        SIGNUP                  = 246,
        PASSWORD_RECOVERY       = 247,
        JOIN_ROOM               = 248,
        JOIN_ROOM_FAILED        = 249,
        CHAT_ROOM               = 250,
        CREATE_ROOM             = 251,
        PLAYER_REMOVED          = 252, // Player removed when send wrong data to server (cheating, ...)
        GLOBAL_CHAT             = 253,
        DISCONNECT_OTHER_LOGIN  = 254,
        EXTENDED_OPERATION      = 255,
    }; // Add ; to copy to C++ easier

#if C_SHARP
    public enum DataCode : byte
#else
    enum class DataCode
#endif
    {
        RESULT                = 242,
        USERNAME              = 243,
        PASSWORD              = 244,
        GAME_CODE             = 245,
        EMAIL                 = 246,
        PHONE_NUMBER          = 247,
        VALUE_ROOM_FULL       = 248,
        ROOMS_NAME            = 249,
        VALUE_ROOM_NOT_EXIST  = 250,
        ROOM_NAME             = 251,
        CHAT_MESSAGE          = 252,
        PLAYERS               = 253,
        REASON                = 254,
        // Data code use in future
        EXTENDED_CODE = 255
    }; // Add ; to copy to C++ easier

#if C_SHARP
    public enum EventCode : byte
#else
    enum class EventCode
#endif
    {
        BROADCAST_JOIN_ROOM     = 252,
        BROADCAST_LEAVE_ROOM    = 253,
        CHAT_ROOM               = 254,
	    EXTENDED_EVENT          = 255
    }; // Add ; to copy to C++ easier

#if C_SHARP
}
#endif