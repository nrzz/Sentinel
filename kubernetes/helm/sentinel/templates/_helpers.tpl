{{/*
Expand the name of the chart.
*/}}
{{- define "sentinel.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "sentinel.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "sentinel.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "sentinel.labels" -}}
helm.sh/chart: {{ include "sentinel.chart" . }}
{{ include "sentinel.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "sentinel.selectorLabels" -}}
app.kubernetes.io/name: {{ include "sentinel.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "sentinel.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "sentinel.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "sentinel.image" -}}
{{- $registry := .Values.global.imageRegistry | default .Values.image.registry -}}
{{- $repository := .repository -}}
{{- $tag := .tag | default $.Chart.AppVersion -}}
{{- if $registry }}
{{- printf "%s/%s:%s" $registry $repository $tag }}
{{- else }}
{{- printf "%s:%s" $repository $tag }}
{{- end }}
{{- end }}

{{- define "sentinel.postgresql.host" -}}
{{- if .Values.externalDatabase.postgresql.enabled }}
{{- .Values.externalDatabase.postgresql.host }}
{{- else }}
{{- printf "%s-postgresql" (include "sentinel.fullname" .) }}
{{- end }}
{{- end }}

{{- define "sentinel.clickhouse.host" -}}
{{- if .Values.externalDatabase.clickhouse.enabled }}
{{- .Values.externalDatabase.clickhouse.host }}
{{- else }}
{{- printf "%s-clickhouse" (include "sentinel.fullname" .) }}
{{- end }}
{{- end }}

{{- define "sentinel.redis.host" -}}
{{- if .Values.externalDatabase.redis.enabled }}
{{- .Values.externalDatabase.redis.host }}
{{- else }}
{{- printf "%s-redis" (include "sentinel.fullname" .) }}
{{- end }}
{{- end }}

{{- define "sentinel.rabbitmq.host" -}}
{{- if .Values.externalDatabase.rabbitmq.enabled }}
{{- .Values.externalDatabase.rabbitmq.host }}
{{- else }}
{{- printf "%s-rabbitmq" (include "sentinel.fullname" .) }}
{{- end }}
{{- end }}

{{- define "sentinel.postgresql.connectionString" -}}
{{- $host := include "sentinel.postgresql.host" . -}}
{{- $port := .Values.externalDatabase.postgresql.port | default .Values.postgresql.service.port -}}
{{- $db := .Values.externalDatabase.postgresql.database | default .Values.postgresql.auth.database -}}
{{- $user := .Values.externalDatabase.postgresql.username | default .Values.postgresql.auth.username -}}
{{- printf "Host=%s;Port=%d;Database=%s;Username=%s;Password=$(POSTGRES_PASSWORD)" $host (int $port) $db $user }}
{{- end }}

{{- define "sentinel.clickhouse.connectionString" -}}
{{- $host := include "sentinel.clickhouse.host" . -}}
{{- $port := .Values.externalDatabase.clickhouse.port | default .Values.clickhouse.service.httpPort -}}
{{- $db := .Values.externalDatabase.clickhouse.database | default .Values.clickhouse.auth.database -}}
{{- $user := .Values.externalDatabase.clickhouse.username | default .Values.clickhouse.auth.username -}}
{{- printf "Host=%s;Port=%d;Database=%s;Username=%s;Password=$(CLICKHOUSE_PASSWORD)" $host (int $port) $db $user }}
{{- end }}

{{- define "sentinel.redis.connectionString" -}}
{{- $host := include "sentinel.redis.host" . -}}
{{- $port := .Values.externalDatabase.redis.port | default .Values.redis.service.port -}}
{{- printf "%s:%d" $host (int $port) }}
{{- end }}

{{- define "sentinel.rabbitmq.connectionString" -}}
{{- $host := include "sentinel.rabbitmq.host" . -}}
{{- $port := .Values.externalDatabase.rabbitmq.port | default .Values.rabbitmq.service.amqpPort -}}
{{- $user := .Values.externalDatabase.rabbitmq.username | default .Values.rabbitmq.auth.username -}}
{{- $vhost := .Values.externalDatabase.rabbitmq.vhost | default "/" -}}
{{- printf "amqp://%s:$(RABBITMQ_PASSWORD)@%s:%d%s" $user $host (int $port) $vhost }}
{{- end }}
