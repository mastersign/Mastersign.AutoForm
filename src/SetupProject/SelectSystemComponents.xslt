<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:wi="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi"
>
    <xsl:output method="xml" indent="yes"/>

    <xsl:template match="/">
      <Include>
        <xsl:for-each select="//wi:Component/wi:File[starts-with(@Source, 'SourceDir\System.')] | //wi:File[starts-with(@Source, 'SourceDir\Microsoft.')]">
          <Component>
            <xsl:attribute name="Id">
              <xsl:value-of select="../@Id"/>
            </xsl:attribute>
            <xsl:attribute name="Guid">
              <xsl:text>*</xsl:text>
            </xsl:attribute>
            <File>
              <xsl:attribute name="Id">
                <xsl:value-of select="@Id"/>
              </xsl:attribute>
              <xsl:attribute name="Source">
                <xsl:text>$(var.Mastersign.AutoForm.TargetDir)</xsl:text>
                <xsl:value-of select="substring-after(@Source, '\')"/>
              </xsl:attribute>
            </File>
          </Component>
        </xsl:for-each>
      </Include>
    </xsl:template>
  
</xsl:stylesheet>
