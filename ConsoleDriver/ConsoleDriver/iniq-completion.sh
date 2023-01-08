#!/usr/bin/env bash

# Increments $current and sets $word to the current word
increment() {
    current=$(( $current + 1 ))
    word="${COMP_WORDS[$current]}"
}

# Splits an array by whitespace into lines
split_array() {
    arr=("$@")
    for value in ${arr[@]}; do
        echo $value
    done
}

# Outputs an array of completions, escaping each element
add_completions() {
    completions=("$@")

    for c in "${completions[@]}"; do
        COMPREPLY+=("$(printf "%q" "$c")")
    done
}

# Saves field separator and changes it to newline
push_separator() {
    IFS_old=$IFS
    IFS=$'\n'
}

# Restores the old field separator
pop_separator() {
    IFS=$IFS_old
}

# TODO move cursor back onto end of partial directory after completing

complete_file() {
    file="$word"
    quotedFile=$(printf "%q" "$file")
    sep=""

    if [ $current -eq "$(( $COMP_CWORD-1 ))" ] && [ -d $word ]; then
        sep=/
    elif [ $COMP_CWORD -ne $current ]; then
        increment complete_file\$skip
        return
    fi

    increment complete_file
    push_separator
    
    completions=($(eval ls -- "$quotedFile$sep*@(ini|conf|properties)" 2>/dev/null))

    if [[ ${#completions[@]} == 0 ]]; then
        completions=($(eval ls -d $quotedFile* 2>/dev/null))
        COMP_CWORD=$(($COMP_CWORD - 1))
    fi
    
    pop_separator
    add_completions "${completions[@]}"
}

complete_section() {
    section=$word

    if [ $COMP_CWORD -ne $current ]; then
        increment complete_section\$skip
        return
    fi

    increment complete_section

    push_separator
    completions=($(eval iniq $options "$file"))

    if [ -n $section ]; then
        # Filter by partial text
        completions=($(printf -- "%s\n" "${completions[@]}" | grep "^$section"))
    fi

    pop_separator
    add_completions "${completions[@]}"
}

complete_key() {
    key=$word

    if [ $COMP_CWORD -ne $current ]; then
        increment complete_key\$skip
        return
    fi

    increment complete_key
    
    # Get keys and then take just the key name
    push_separator
    completions=($(eval iniq $options "$file" "$section" | awk -F '=' '{ print $1; }'))
    
    if [ -n $key ]; then
        # Filter by partial text
        completions=($(printf -- "%s\n" "${completions[@]}" | grep "^$key"))
    fi

    pop_separator
    add_completions "${completions[@]}"
}

complete_rule_option() {
    if [ $COMP_CWORD -ne $current ]; then
        increment complete_rule_option\$skip
        return
    fi

    completions=("$@")

    if [ -n $word ]; then
        # Filter by partial text
        completions=( $(split_array "${completions[@]}" | grep "^$word") )
    fi

    COMPREPLY+=(${completions[@]})

    increment complete_rule_option
}

complete_switch_argument() {
    while true; do
        case $word in
            "-C" | "--commentRule")
                if [ -z $COMMENT_RULES ]; then
                    COMMENT_RULES=($(iniq -C | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${COMMENT_RULES[@]}"
                ;;
            "-K" | "--duplicateKeyRule")
                if [ -z $DUPLICATE_KEY_RULES ]; then
                    DUPLICATE_KEY_RULES=($(iniq -K | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${DUPLICATE_KEY_RULES[@]}"
                ;;
            "-T" | "--duplicateSectionRule")
                if [ -z $DUPLICATE_SECTION_RULES ]; then
                    DUPLICATE_SECTION_RULES=($(iniq -T | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${DUPLICATE_SECTION_RULES[@]}"
                ;;
            "-E" | "--escapeSequenceRule")
                if [ -z $ESCAPE_SEQUENCE_RULES ]; then
                    ESCAPE_SEQUENCE_RULES=($(iniq -E | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${ESCAPE_SEQUENCE_RULES[@]}"
                ;;
            "-G" | "--globalKeysRule")
                if [ -z $GLOBAL_KEYS_RULES ]; then
                    GLOBAL_KEYS_RULES=($(iniq -G | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${GLOBAL_KEYS_RULES[@]}"
                ;;
            "-D" | "--nameValueDelimiterRule")
                if [ -z $DELIMITER_RULES ]; then
                    DELIMITER_RULES=($(iniq -D | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${DELIMITER_RULES[@]}"
                ;;
            "-P" | "--nameValueDelimiterPreferenceRule")
                if [ -z $DELIMITER_PREFERENCE_RULES ]; then
                    DELIMITER_PREFERENCE_RULES=($(iniq -P | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${DELIMITER_PREFERENCE_RULES[@]}"
                ;;
            "-S" | "--nameValueDelimiterSpacingRule")
                if [ -z $DELIMITER_SPACING_RULES ]; then
                    DELIMITER_SPACING_RULES=($(iniq -S | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${DELIMITER_SPACING_RULES[@]}"
                ;;
            "-N" | "--newlineRule")
                if [ -z $NEWLINE_RULES ]; then
                    NEWLINE_RULES=($(iniq -N | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${NEWLINE_RULES[@]}"
                ;;
            "-Q" | "--quotationRule")
                if [ -z $QUOTATION_RULES ]; then
                    QUOTATION_RULES=($(iniq -Q | awk '{ print $1; } '))
                fi
                increment complete_switches
                complete_rule_option "${QUOTATION_RULES[@]}"
                ;;
            "-o" | "--outputPath")
                increment complete_switches
                complete_file
                ;;
            "-n" | "-c")
                increment complete_switches
                ;;
            "--")
                break
                ;;
            *)
                break
                ;;
        esac
        
        last_switch_index=$current
    done

    if [ $last_switch_index -gt 0 ]; then
        options=${COMP_WORDS[@]:1:$last_switch_index-1}
    else
        options=()
    fi
}

complete_switch() {    
    if [ $current -lt $COMP_CWORD ]; then
        return
    fi

    # Get the switches, separating short and long forms into separate lines
    completions=($(iniq -h | grep "^-" | awk 'match($0, "(-[a-zA-Z]), (--[a-zA-Z]+)", m) { print m[1]; print m[2]; }'))

    if [ -n $word ]; then
        # Filter by partial text
        completions=( $(split_array "${completions[@]}" | grep "^$word") )
    fi

    if [ -z $completions ]; then
        # No match - skip
        increment complete_switch\$skip
        return
    fi

    COMPREPLY+=(${completions[@]})
}

_iniq_completions() {
    INIQ_DEFAULT_EXTENSIONS="ini|conf|properties"
    INIQ_EXTENSIONS=${INIQ_EXTENSIONS:-$INIQ_DEFAULT_EXTENSIONS}
    current=0
    length=${#COMP_WORDS[@]}
    last_switch_index=0
    last_index=$(( $length - 1 ))
    options=()

    increment _iniq_completions

    # Keep skipping over switches until one is finally processed, or we've skipped over all of them
    while [[ $word == "-"* && ${#COMPREPLY[@]} == 0 ]]; do
        complete_switch
        complete_switch_argument

        if [[ $word == "--" && $COMP_CWORD > $current ]]; then
            # User typed past --, skip past it and start processing the operands
            increment _iniq_completions\$while
            break
        fi
    done
    
    complete_file
    complete_section
    complete_key
}

complete -F _iniq_completions iniq
