using EnumStringValues;

namespace SimpleDrawing
{
    public enum CommandEnum
    {
        //Controller Commands
        [StringValue("MSG")]
        MESSAGE,
        [StringValue("DRW")]
        DRAWING,
        [StringValue("CLR")]
        CLEAR,

        // Game Service Commands
        [StringValue("ADR")]
        ADD_USER_REQUEST,
        [StringValue("IGR")]
        INTIAL_GAME_REQUEST,
        [StringValue("SGR")]
        START_GAME_REQUEST,
        [StringValue("SGA")]
        START_GAME_ACKNOWLEDGEMENT,
        [StringValue("SGN")]
        START_GAME_NOTACKNOWLEDGEMENT,
        [StringValue("GSR")]
        GUESSER_REQUEST,
        [StringValue("DWR")]
        DRAWER_REQUEST,
        [StringValue("DWA")]
        DRAWER_ACKNOWLEDGEMENT,
        [StringValue("RSR")]
        ROUND_START_REQUEST,
        [StringValue("RSA")]
        ROUND_START_ACKNOWLEDGEMENT,
        [StringValue("RSN")]
        ROUND_START_NOTACKNOWLEDGEMENT,
        [StringValue("RST")]
        ROUND_STARTED,
        [StringValue("CLG")]
        CLOSE_GUESS,
        [StringValue("CRG")]
        CORRECT_GUESS,
        [StringValue("RES")]
        ROUND_END_SUCCESS,
        [StringValue("RET")]
        ROUND_END_TIMEOUT,
        [StringValue("REA")]
        ROUND_END_ACKNOWLEDGEMENT,
        [StringValue("ERR")]
        ERROR
    }
}
