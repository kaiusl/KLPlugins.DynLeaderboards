items = ["AC", "ACC", "AMS2", "RF2", "R3E"]
itemsFull = [
    "Assetto Corsa",
    "Assetto Corsa Competizione",
    "Automobilista 2",
    "rFactor2",
    "RaceRoom Racing Experience",
]


def define_env(env):
    """
    This is the hook for the functions (new form)
    """

    versionIcon = """<span class="version-icon">:material-tag-outline:</span>"""
    defValueIcon = ":material-numeric-off:"
    env.variables.versionIcon = versionIcon
    env.variables.defValueIcon = defValueIcon

    @env.macro
    def sinceVersion(version, right=True):
        tooltip = f"Available since version {version}."
        right = "right" if right else ""
        return f"""<a href="https://github.com/kaiusl/KLPlugins.DynLeaderboards/releases/tag/v{version}"><span class="version-since {right}" title="{tooltip}">{versionIcon}{version}</span></a>"""

    @env.macro
    def supportedGames(*games):
        result = ""

        isAll = "all" in games

        for i, (game, gameFull) in enumerate(zip(items, itemsFull)):
            isDisabled = (not isAll and game not in games) or f"!{game}" in games
            disabled = "disabled" if isDisabled else ""
            title = (
                f"Not available for {gameFull}."
                if isDisabled
                else f"Available for {gameFull}."
            )

            if i > 0:
                result += " "
            result += (
                f"""<span class="pill game {disabled}" title="{title}">{game}</span>"""
            )

        return result

    @env.macro
    def prop(
        name, version, games, defv=None, ty="Any", deprecated=None, deprecatedTooltip=""
    ):
        supported = supportedGames(*games)
        defv = (
            ""
            if defv is None
            else f"""<span style="float:right; margin-right:10px;" title="Default value.">:material-numeric-off: `{defv}`</span>"""
        )
        deprecated = (
            ""
            if deprecated is None
            else f"""<span class="pill pill-deprecated" title="{deprecatedTooltip}">Deprecated {versionIcon}{deprecated}</span>"""
        )

        return f"""**`{name}`**: `{ty}`{deprecated} {sinceVersion(version)}{defv}<br><span style="float:right">{supported}</span>"""

    @env.macro
    def path(path):
        return f"""*`\"{path}\"`*"""

    @env.macro
    def dashTitle(name, author):
        return f"""<span style="font-weight:bold;font-size:125%;">{name}</span> by *{author}*"""

    def leaderboardTypePreview(kind):
        return f"""
=== "{kind}"
    ![](../img/LeaderboardTypes/{kind}.png){{: style="height:400px;margin-left:auto;margin-right:auto;display:block;"}}
"""

    @env.macro
    def leaderboardTypePreviews():
        LeaderboardTypes = [
            "Overall", "Class", "Cup", 
            "RelativeOverall", "RelativeClass", "RelativeCup", 
            "PartialRelativeOverall", "PartialRelativeClass", "PartialRelativeCup", 
            "RelativeOnTrack", "RelativeOnTrackWoPit"
        ]
        return "\n\n".join(leaderboardTypePreview(kind) for kind in LeaderboardTypes)