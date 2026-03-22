import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildFeatures.perfmon
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetBuild
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetPublish
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetRestore
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetTest
import jetbrains.buildServer.configs.kotlin.buildSteps.script

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.

VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.

To debug settings scripts in command-line, run the

    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate

command and attach your debugger to the port 8000.

To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2025.11"

project {

    buildType(Build)
}

object Build : BuildType({
    name = "Build"

    publishArtifacts = PublishMode.SUCCESSFUL

    params {
        param("env.DotNetCLI_Path", "/usr/bin/dotnet")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        dotnetRestore {
            name = "restore"
            id = "restore"
            projects = "Payslip4All.sln"
        }
        dotnetBuild {
            name = "Build"
            id = "Build"
            projects = "Payslip4All.sln"
            configuration = "Release"
        }
        dotnetTest {
            name = "Tests"
            id = "Tests"
            projects = "Payslip4All.sln"
            configuration = "Release"
        }
        script {
            name = "Stop App"
            id = "Stop_App"
            scriptContent = """pkill -cf "dotnet /home/apps/payslip4all/Payslip4All.Web.dll" || true"""
        }
        script {
            name = "Clean"
            id = "Clean"
            scriptContent = """
                whoami
                rm -Rf /webapps/payslip4all/
                mkdir /webapps/payslip4all
            """.trimIndent()
        }
        dotnetPublish {
            name = "Publish"
            id = "Publish"
            projects = "Payslip4All.sln"
            configuration = "Release"
            args = "-o /home/apps/payslip4all"
        }
        script {
            name = "Start App"
            id = "Start_App"
            scriptContent = """nohup ${'$'}(dotnet /webapps/payslip4all/Payslip4All.Web.dll --urls="http://localhost:5000") &"""
        }
    }

    features {
        perfmon {
        }
    }
})
