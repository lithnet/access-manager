<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">
  <xsl:output method="xml" indent="yes" />
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>
  <xsl:key name="exe-search" match="wix:Component[contains(wix:File/@Source, '.exe')]" use="@Id" />
  <xsl:template match="wix:Component[key('exe-search', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('exe-search', @Id)]" />

  <xsl:key name="templates-search" match="wix:Component[contains(wix:File/@Source, 'NotificationTemplates')]" use="@Id"/>
  <xsl:template match="wix:Component[key('templates-search', @Id)]"/>
  <xsl:template match="wix:ComponentRef[key('templates-search', @Id)]"/>

  <xsl:key name="scripts-search" match="wix:Component[contains(wix:File/@Source, 'Scripts')]" use="@Id"/>
  <xsl:template match="wix:Component[key('scripts-search', @Id)]"/>
  <xsl:template match="wix:ComponentRef[key('scripts-search', @Id)]"/>

  <xsl:key name="appsettings-search" match="wix:Component[contains(wix:File/@Source, 'appsettings.json')]" use="@Id"/>
  <xsl:template match="wix:Component[key('appsettings-search', @Id)]"/>
  <xsl:template match="wix:ComponentRef[key('appsettings-search', @Id)]"/>

  <xsl:key name="targets-search" match="wix:Component[contains(wix:File/@Source, 'targets.json')]" use="@Id"/>
  <xsl:template match="wix:Component[key('targets-search', @Id)]"/>
  <xsl:template match="wix:ComponentRef[key('targets-search', @Id)]"/>

</xsl:stylesheet>