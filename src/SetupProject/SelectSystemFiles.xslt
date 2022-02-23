<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:wi="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
>
    <xsl:output method="xml" indent="yes"/>

    <xsl:template match="/">
      <Include>
        <xsl:for-each select="//wi:Component/wi:File[contains(@Source, '\System.')] | //wi:File[contains(@Source, '\Microsoft.')]">
          <File>
            <xsl:attribute name="Id">
              <xsl:value-of select="@Id"/>
            </xsl:attribute>
            <xsl:attribute name="Source">
              <xsl:value-of select="@Source"/>
            </xsl:attribute>          
          </File>
        </xsl:for-each>
      </Include>
    </xsl:template>
  
</xsl:stylesheet>
