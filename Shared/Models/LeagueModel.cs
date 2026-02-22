using System.Text.Json.Serialization;

namespace Shared.Models;

public sealed class LeagueModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("metadata")]
    public LeagueMetadataModel? Metadata { get; set; }

    [JsonPropertyName("settings")]
    public LeagueSettingsModel? Settings { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("company_id")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("shard")]
    public int? Shard { get; set; }

    [JsonPropertyName("season")]
    public string? Season { get; set; }

    [JsonPropertyName("season_type")]
    public string? SeasonType { get; set; }

    [JsonPropertyName("sport")]
    public string? Sport { get; set; }

    [JsonPropertyName("scoring_settings")]
    public LeagueScoringSettingsModel? ScoringSettings { get; set; }

    [JsonPropertyName("last_message_id")]
    public string? LastMessageId { get; set; }

    [JsonPropertyName("last_author_avatar")]
    public string? LastAuthorAvatar { get; set; }

    [JsonPropertyName("last_author_display_name")]
    public string? LastAuthorDisplayName { get; set; }

    [JsonPropertyName("draft_id")]
    public string? DraftId { get; set; }

    [JsonPropertyName("last_author_id")]
    public string? LastAuthorId { get; set; }

    [JsonPropertyName("last_author_is_bot")]
    public bool? LastAuthorIsBot { get; set; }

    [JsonPropertyName("last_message_attachment")]
    public string? LastMessageAttachment { get; set; }

    [JsonPropertyName("last_message_text_map")]
    public string? LastMessageTextMap { get; set; }

    [JsonPropertyName("last_message_time")]
    public long? LastMessageTime { get; set; }

    [JsonPropertyName("last_pinned_message_id")]
    public string? LastPinnedMessageId { get; set; }

    [JsonPropertyName("last_read_id")]
    public string? LastReadId { get; set; }

    [JsonPropertyName("league_id")]
    public string? LeagueId { get; set; }

    [JsonPropertyName("previous_league_id")]
    public string? PreviousLeagueId { get; set; }

    [JsonPropertyName("bracket_id")]
    public long? BracketId { get; set; }

    [JsonPropertyName("bracket_overrides_id")]
    public long? BracketOverridesId { get; set; }

    [JsonPropertyName("group_id")]
    public long? GroupId { get; set; }

    [JsonPropertyName("loser_bracket_id")]
    public long? LoserBracketId { get; set; }

    [JsonPropertyName("loser_bracket_overrides_id")]
    public long? LoserBracketOverridesId { get; set; }

    [JsonPropertyName("roster_positions")]
    public List<string>? RosterPositions { get; set; }

    [JsonPropertyName("total_rosters")]
    public int TotalRosters { get; set; }
}

public sealed class LeagueMetadataModel
{
    [JsonPropertyName("auto_continue")]
    public string? AutoContinue { get; set; }

    [JsonPropertyName("continued")]
    public string? Continued { get; set; }

    [JsonPropertyName("keeper_deadline")]
    public string? KeeperDeadline { get; set; }

    [JsonPropertyName("latest_league_winner_roster_id")]
    public string? LatestLeagueWinnerRosterId { get; set; }
}

public sealed class LeagueSettingsModel
{
    [JsonPropertyName("best_ball")]
    public int BestBall { get; set; }

    [JsonPropertyName("last_report")]
    public int LastReport { get; set; }

    [JsonPropertyName("waiver_budget")]
    public int WaiverBudget { get; set; }

    [JsonPropertyName("disable_adds")]
    public int DisableAdds { get; set; }

    [JsonPropertyName("capacity_override")]
    public int CapacityOverride { get; set; }

    [JsonPropertyName("waiver_bid_min")]
    public int WaiverBidMin { get; set; }

    [JsonPropertyName("taxi_deadline")]
    public int TaxiDeadline { get; set; }

    [JsonPropertyName("draft_rounds")]
    public int DraftRounds { get; set; }

    [JsonPropertyName("reserve_allow_na")]
    public int ReserveAllowNa { get; set; }

    [JsonPropertyName("start_week")]
    public int StartWeek { get; set; }

    [JsonPropertyName("playoff_seed_type")]
    public int PlayoffSeedType { get; set; }

    [JsonPropertyName("playoff_teams")]
    public int PlayoffTeams { get; set; }

    [JsonPropertyName("veto_votes_needed")]
    public int VetoVotesNeeded { get; set; }

    [JsonPropertyName("num_teams")]
    public int NumTeams { get; set; }

    [JsonPropertyName("daily_waivers_hour")]
    public int DailyWaiversHour { get; set; }

    [JsonPropertyName("playoff_type")]
    public int PlayoffType { get; set; }

    [JsonPropertyName("taxi_slots")]
    public int TaxiSlots { get; set; }

    [JsonPropertyName("sub_start_time_eligibility")]
    public int SubStartTimeEligibility { get; set; }

    [JsonPropertyName("last_scored_leg")]
    public int LastScoredLeg { get; set; }

    [JsonPropertyName("daily_waivers_days")]
    public int DailyWaiversDays { get; set; }

    [JsonPropertyName("sub_lock_if_starter_active")]
    public int SubLockIfStarterActive { get; set; }

    [JsonPropertyName("playoff_week_start")]
    public int PlayoffWeekStart { get; set; }

    [JsonPropertyName("waiver_clear_days")]
    public int WaiverClearDays { get; set; }

    [JsonPropertyName("reserve_allow_doubtful")]
    public int ReserveAllowDoubtful { get; set; }

    [JsonPropertyName("commissioner_direct_invite")]
    public int CommissionerDirectInvite { get; set; }

    [JsonPropertyName("veto_auto_poll")]
    public int VetoAutoPoll { get; set; }

    [JsonPropertyName("reserve_allow_dnr")]
    public int ReserveAllowDnr { get; set; }

    [JsonPropertyName("taxi_allow_vets")]
    public int TaxiAllowVets { get; set; }

    [JsonPropertyName("waiver_day_of_week")]
    public int WaiverDayOfWeek { get; set; }

    [JsonPropertyName("playoff_round_type")]
    public int PlayoffRoundType { get; set; }

    [JsonPropertyName("reserve_allow_out")]
    public int ReserveAllowOut { get; set; }

    [JsonPropertyName("reserve_allow_sus")]
    public int ReserveAllowSus { get; set; }

    [JsonPropertyName("veto_show_votes")]
    public int VetoShowVotes { get; set; }

    [JsonPropertyName("trade_deadline")]
    public int TradeDeadline { get; set; }

    [JsonPropertyName("taxi_years")]
    public int TaxiYears { get; set; }

    [JsonPropertyName("daily_waivers")]
    public int DailyWaivers { get; set; }

    [JsonPropertyName("faab_suggestions")]
    public int FaabSuggestions { get; set; }

    [JsonPropertyName("disable_trades")]
    public int DisableTrades { get; set; }

    [JsonPropertyName("pick_trading")]
    public int PickTrading { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("max_keepers")]
    public int MaxKeepers { get; set; }

    [JsonPropertyName("waiver_type")]
    public int WaiverType { get; set; }

    [JsonPropertyName("max_subs")]
    public int MaxSubs { get; set; }

    [JsonPropertyName("league_average_match")]
    public int LeagueAverageMatch { get; set; }

    [JsonPropertyName("trade_review_days")]
    public int TradeReviewDays { get; set; }

    [JsonPropertyName("bench_lock")]
    public int BenchLock { get; set; }

    [JsonPropertyName("offseason_adds")]
    public int OffseasonAdds { get; set; }

    [JsonPropertyName("leg")]
    public int Leg { get; set; }

    [JsonPropertyName("reserve_slots")]
    public int ReserveSlots { get; set; }

    [JsonPropertyName("reserve_allow_cov")]
    public int ReserveAllowCov { get; set; }

    [JsonPropertyName("daily_waivers_last_ran")]
    public int DailyWaiversLastRan { get; set; }
}

public sealed class LeagueScoringSettingsModel
{
    [JsonPropertyName("sack")]
    public double Sack { get; set; }

    [JsonPropertyName("fgm_40_49")]
    public double Fgm40To49 { get; set; }

    [JsonPropertyName("pass_int")]
    public double PassInt { get; set; }

    [JsonPropertyName("pts_allow_0")]
    public double PtsAllow0 { get; set; }

    [JsonPropertyName("pass_2pt")]
    public double Pass2Pt { get; set; }

    [JsonPropertyName("st_td")]
    public double StTd { get; set; }

    [JsonPropertyName("rec_td")]
    public double RecTd { get; set; }

    [JsonPropertyName("fgm_30_39")]
    public double Fgm30To39 { get; set; }

    [JsonPropertyName("fgm_50_59")]
    public double Fgm50To59 { get; set; }

    [JsonPropertyName("xpmiss")]
    public double XpMiss { get; set; }

    [JsonPropertyName("rush_td")]
    public double RushTd { get; set; }

    [JsonPropertyName("rec_2pt")]
    public double Rec2Pt { get; set; }

    [JsonPropertyName("st_fum_rec")]
    public double StFumRec { get; set; }

    [JsonPropertyName("fgmiss")]
    public double FgMiss { get; set; }

    [JsonPropertyName("ff")]
    public double Ff { get; set; }

    [JsonPropertyName("rec")]
    public double Rec { get; set; }

    [JsonPropertyName("pts_allow_14_20")]
    public double PtsAllow14To20 { get; set; }

    [JsonPropertyName("fgm_0_19")]
    public double Fgm0To19 { get; set; }

    [JsonPropertyName("int")]
    public double Int { get; set; }

    [JsonPropertyName("def_st_fum_rec")]
    public double DefStFumRec { get; set; }

    [JsonPropertyName("fum_lost")]
    public double FumLost { get; set; }

    [JsonPropertyName("pts_allow_1_6")]
    public double PtsAllow1To6 { get; set; }

    [JsonPropertyName("fgm_20_29")]
    public double Fgm20To29 { get; set; }

    [JsonPropertyName("pts_allow_21_27")]
    public double PtsAllow21To27 { get; set; }

    [JsonPropertyName("xpm")]
    public double Xpm { get; set; }

    [JsonPropertyName("rush_2pt")]
    public double Rush2Pt { get; set; }

    [JsonPropertyName("fum_rec")]
    public double FumRec { get; set; }

    [JsonPropertyName("def_st_td")]
    public double DefStTd { get; set; }

    [JsonPropertyName("fgm_50p")]
    public double Fgm50Plus { get; set; }

    [JsonPropertyName("def_td")]
    public double DefTd { get; set; }

    [JsonPropertyName("safe")]
    public double Safe { get; set; }

    [JsonPropertyName("pass_yd")]
    public double PassYd { get; set; }

    [JsonPropertyName("blk_kick")]
    public double BlkKick { get; set; }

    [JsonPropertyName("pass_td")]
    public double PassTd { get; set; }

    [JsonPropertyName("rush_yd")]
    public double RushYd { get; set; }

    [JsonPropertyName("fum")]
    public double Fum { get; set; }

    [JsonPropertyName("pts_allow_28_34")]
    public double PtsAllow28To34 { get; set; }

    [JsonPropertyName("pts_allow_35p")]
    public double PtsAllow35Plus { get; set; }

    [JsonPropertyName("fum_rec_td")]
    public double FumRecTd { get; set; }

    [JsonPropertyName("rec_yd")]
    public double RecYd { get; set; }

    [JsonPropertyName("def_st_ff")]
    public double DefStFf { get; set; }

    [JsonPropertyName("pts_allow_7_13")]
    public double PtsAllow7To13 { get; set; }

    [JsonPropertyName("st_ff")]
    public double StFf { get; set; }
}
